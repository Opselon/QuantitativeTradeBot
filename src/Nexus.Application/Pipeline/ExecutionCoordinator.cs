using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;
using Nexus.Application.Observability;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Application.Pipeline
{
    public class ExecutionCoordinator
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExecutionGateway _executionGateway;
        private readonly PreTradeRiskEvaluator _riskEvaluator;
        private readonly OrderIntentFactory _intentFactory;
        private readonly ExecutionAuditService _auditService;
        private readonly ILogger<ExecutionCoordinator> _logger;

        public ExecutionCoordinator(
            IAccountRepository accountRepository,
            IOrderRepository orderRepository,
            IPositionRepository positionRepository,
            IUnitOfWork unitOfWork,
            IExecutionGateway executionGateway,
            PreTradeRiskEvaluator riskEvaluator,
            OrderIntentFactory intentFactory,
            ExecutionAuditService auditService,
            ILogger<ExecutionCoordinator> logger)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _executionGateway = executionGateway ?? throw new ArgumentNullException(nameof(executionGateway));
            _riskEvaluator = riskEvaluator ?? throw new ArgumentNullException(nameof(riskEvaluator));
            _intentFactory = intentFactory ?? throw new ArgumentNullException(nameof(intentFactory));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExecutionResult> ProcessSignalAsync(TradeSignal signal, PipelineContext context, CancellationToken cancellationToken = default)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (context == null) throw new ArgumentNullException(nameof(context));

            string corrId = context.CorrelationId;
            var workflowContext = WorkflowContext.Create("SignalExecution", corrId, subsystem: "Execution");
            workflowContext.StrategyId = signal.StrategyId;
            workflowContext.Symbol = signal.SymbolName;

            using var scope = _logger.BeginWorkflowScope(workflowContext);

            _auditService.LogSignalReceived(signal, corrId);

            // 1. Create intent
            var intent = _intentFactory.CreateIntent(signal);
            _logger.LogStructured(LogLevel.Information, LogEventIds.SignalEmitted, "Created trade intent: IntentId={IntentId}", intent.IntentId);

            // 2. Validate basic input fields
            if (string.IsNullOrWhiteSpace(signal.SymbolName) || signal.Volume <= 0 || signal.Price <= 0)
            {
                string errMsg = "Invalid signal parameters (missing symbol, volume, or price).";
                _logger.LogStructured(LogLevel.Warning, LogEventIds.ValidationRejected, "Signal validation failed: {Msg}", errMsg);
                return new ExecutionResult(intent.IntentId, string.Empty, false, errMsg, 0, 0, DateTime.UtcNow);
            }

            // 3. Create domain Order entity (Initially Pending)
            var symbol = new Symbol(signal.SymbolName);
            var lotSize = new LotSize(signal.Volume);
            var order = Order.CreateNew(symbol, signal.Direction, signal.Type, lotSize, signal.Price, signal.StopLoss, signal.TakeProfit);

            try
            {
                // Save initial order status as pending in repository
                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _auditService.LogOrderSubmitted(order.Id, symbol.Name, corrId);

                _logger.LogStructured(LogLevel.Information, LogEventIds.OrderSubmitted, "Order submitted: OrderId={OrderId} Symbol={Symbol}", order.Id, symbol.Name);

                // 4. Retrieve Account Status (Default/First active account)
                // For E2E or real runtime, we can resolve accounts by Id or get a default account
                var account = await _accountRepository.GetByIdAsync("DEFAULT_ACCOUNT")
                    ?? await _accountRepository.GetByIdAsync("ACC_12345");

                if (account == null)
                {
                    string errMsg = "Active broker account not found.";
                    order.Reject(errMsg);
                    await _orderRepository.UpdateAsync(order);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _auditService.LogRiskEvaluated(signal.StrategyId, symbol.Name, false, errMsg, corrId);

                    _logger.LogStructured(LogLevel.Warning, LogEventIds.RiskRejected, "Pre-trade risk failed: Account not found.");
                    return new ExecutionResult(intent.IntentId, string.Empty, false, errMsg, 0, 0, DateTime.UtcNow);
                }

                workflowContext.AccountId = account.BrokerAccountId;

                // 5. Pre-Trade Risk Checks
                var riskDecision = await _riskEvaluator.EvaluateAsync(account, order);
                _auditService.LogRiskEvaluated(signal.StrategyId, symbol.Name, riskDecision.IsPassed, riskDecision.Reason, corrId);

                if (!riskDecision.IsPassed)
                {
                    order.Reject(riskDecision.Reason);
                    await _orderRepository.UpdateAsync(order);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogStructured(LogLevel.Warning, LogEventIds.RiskRejected, "Risk check REJECTED: {Reason}", riskDecision.Reason);
                    return new ExecutionResult(intent.IntentId, string.Empty, false, $"Risk Rejected: {riskDecision.Reason}", 0, 0, DateTime.UtcNow);
                }

                // 6. Build Execution Command & Dispatch to Gateway (MT5)
                var command = new ExecutionCommand(
                    Guid.NewGuid(),
                    account.BrokerAccountId,
                    symbol.Name,
                    order.Direction,
                    order.Type,
                    order.Volume.Value,
                    order.Price,
                    order.StopLoss,
                    order.TakeProfit,
                    order.Id.ToString("N")
                );

                _logger.LogStructured(LogLevel.Information, LogEventIds.OrderSubmitted, "Dispatching order execution command to Gateway... AccountId={AccountId}", account.BrokerAccountId);
                var report = await _executionGateway.ExecuteAsync(command, cancellationToken);

                // 7. Update order status based on Gateway Execution Report
                if (report.IsSuccess)
                {
                    order.Fill(report.TicketId, report.ExecutionPrice);
                    await _orderRepository.UpdateAsync(order);

                    // Create/Update Open Position
                    var positionId = Guid.NewGuid();
                    var position = new Position(
                        positionId,
                        report.TicketId,
                        symbol,
                        order.Direction,
                        order.Volume,
                        report.ExecutionPrice,
                        report.ExecutionPrice, // Current price initialized to execution price
                        order.StopLoss,
                        order.TakeProfit
                    );
                    await _positionRepository.AddAsync(position);

                    // Update account equity and free margin (simple simulation)
                    decimal marginCost = (decimal)(report.ExecutionPrice * report.ExecutedVolume * 100); // simple cost calculation
                    account.UpdateBalanceAndEquity(account.Balance, account.Equity, account.Margin + marginCost, account.FreeMargin - marginCost);
                    await _accountRepository.UpsertAsync(account);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _auditService.LogOrderExecutionResult(order.Id, report.TicketId, true, "Success", corrId);

                    _logger.LogStructured(LogLevel.Information, LogEventIds.OrderFilled, "Order successfully FILLED on Gateway. TicketId={TicketId} Price={Price}", report.TicketId, report.ExecutionPrice);

                    return new ExecutionResult(intent.IntentId, report.TicketId, true, string.Empty, report.ExecutionPrice, report.ExecutedVolume, report.Timestamp);
                }
                else
                {
                    order.Reject(report.ErrorMessage);
                    await _orderRepository.UpdateAsync(order);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _auditService.LogOrderExecutionResult(order.Id, string.Empty, false, report.ErrorMessage, corrId);

                    _logger.LogStructured(LogLevel.Warning, LogEventIds.OrderRejected, "Order REJECTED by Gateway: {Reason}", report.ErrorMessage);

                    return new ExecutionResult(intent.IntentId, string.Empty, false, report.ErrorMessage, 0, 0, report.Timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Unexpected error in Execution Pipeline.");
                order.Reject(ex.Message);
                try
                {
                    await _orderRepository.UpdateAsync(order);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // Ignore DB secondary failures to retain primary exception
                }
                return new ExecutionResult(intent.IntentId, string.Empty, false, $"Pipeline Exception: {ex.Message}", 0, 0, DateTime.UtcNow);
            }
        }
    }
}
