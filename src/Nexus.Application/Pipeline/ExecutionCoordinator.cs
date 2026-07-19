using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Application.Dashboard;
using Nexus.Application.Observability;
using Nexus.Application.Ports;
using Nexus.Core.DomainEvents;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Core.ValueObjects;

namespace Nexus.Application.Pipeline
{
    public class ExecutionCoordinator : IDisposable
    {
        #region Private Fields & Dependencies
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PreTradeRiskEvaluator _riskEvaluator;
        private readonly OrderIntentFactory _intentFactory;
        private readonly ExecutionAuditService _auditService;
        private readonly ILogger<ExecutionCoordinator> _logger;

        private readonly IDecisionEventStream _decisionStream;
        private readonly IExecutionDashboardService _executionDashboardService;
        private readonly IMarketDashboardService _marketDashboardService;
        #endregion

        #region Constructor
        public ExecutionCoordinator(
            IServiceScopeFactory scopeFactory,
            PreTradeRiskEvaluator riskEvaluator,
            OrderIntentFactory intentFactory,
            ExecutionAuditService auditService,
            ILogger<ExecutionCoordinator> logger,
            IDecisionEventStream decisionStream,
            IExecutionDashboardService executionDashboardService,
            IMarketDashboardService marketDashboardService)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _riskEvaluator = riskEvaluator ?? throw new ArgumentNullException(nameof(riskEvaluator));
            _intentFactory = intentFactory ?? throw new ArgumentNullException(nameof(intentFactory));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _decisionStream = decisionStream ?? throw new ArgumentNullException(nameof(decisionStream));
            _executionDashboardService = executionDashboardService ?? throw new ArgumentNullException(nameof(executionDashboardService));
            _marketDashboardService = marketDashboardService ?? throw new ArgumentNullException(nameof(marketDashboardService));

            // Subscribe to AI Decision Engine Stream for Auto-Trade
            _decisionStream.OnDecisionCreated += HandleDecisionCreated;
        }
        #endregion

        #region Event Interceptor (Auto-Trade Gate)
        private void HandleDecisionCreated(DecisionCreatedEvent @event)
        {
            // Decouple the execution from the AI thread to maintain microsecond performance
            _ = Task.Run(async () =>
            {
                try
                {
                    if (string.Equals(@event.Action, "WAIT", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(@event.Action, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    string currentProfile = _executionDashboardService.CurrentProfile.ToString();
                    bool isLiveAllowed = _executionDashboardService.IsLivePermissionGranted;

                    if (string.Equals(currentProfile, "Live", StringComparison.OrdinalIgnoreCase) && !isLiveAllowed)
                    {
                        _logger.LogWarning(LogEventIds.RiskRejected, "[Auto-Trade Security] Execution rejected: Profile is LIVE but explicit live trading permission is missing. Action: {Action} on {Symbol}", @event.Action, @event.Symbol);
                        return;
                    }

                    _logger.LogInformation("[Auto-Trade Orchestrator] Intercepted AI Decision. Initiating automated execution pipeline for {Symbol} -> {Action}", @event.Symbol, @event.Action);

                    double currentPrice = 0.0;
                    if (_marketDashboardService.RecentPrices != null && _marketDashboardService.RecentPrices.Count > 0)
                    {
                        currentPrice = _marketDashboardService.RecentPrices.LastOrDefault();
                    }

                    if (currentPrice <= 0) currentPrice = 1.0;

                    bool isBuy = string.Equals(@event.Action, "BUY", StringComparison.OrdinalIgnoreCase);
                    OrderDirection direction = isBuy ? OrderDirection.Buy : OrderDirection.Sell;

                    var signal = new TradeSignal(
                        StrategyId: "AI_DECISION_ENGINE_V1",
                        SymbolName: @event.Symbol,
                        Direction: direction,
                        Type: OrderType.Market,
                        Volume: 0.01,
                        Price: currentPrice,
                        StopLoss: null,
                        TakeProfit: null
                    );

                    string corrId = $"AUTO-{@event.DecisionId.ToString().Substring(0, 8).ToUpper()}";
                    var context = new PipelineContext(corrId, signal.StrategyId);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Gateway execution timeout boundary
                    await ProcessSignalAsync(signal, context, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Auto-Trade Orchestrator] Critical exception occurred while processing AI decision event ID: {DecisionId}", @event.DecisionId);
                }
            });
        }
        #endregion

        #region Transactional Signal Processing Engine
        public async Task<ExecutionResult> ProcessSignalAsync(TradeSignal signal, PipelineContext context, CancellationToken cancellationToken = default)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (context == null) throw new ArgumentNullException(nameof(context));

            string corrId = context.CorrelationId;
            var workflowContext = WorkflowContext.Create("SignalExecution", corrId, subsystem: "Execution");
            workflowContext.StrategyId = signal.StrategyId;
            workflowContext.Symbol = signal.SymbolName;

            using var scopeLogger = _logger.BeginWorkflowScope(workflowContext);

            _auditService.LogSignalReceived(signal, corrId);

            // 1. Create intent
            var intent = _intentFactory.CreateIntent(signal);
            _logger.LogStructured(LogLevel.Information, LogEventIds.SignalEmitted, "Created trade intent: IntentId={IntentId}", intent.IntentId);

            // 2. Validate basic input fields
            if (string.IsNullOrWhiteSpace(signal.SymbolName) || signal.Volume <= 0 || signal.Price < 0)
            {
                string errMsg = "Invalid signal parameters (missing symbol, negative volume, or invalid price).";
                _logger.LogStructured(LogLevel.Warning, LogEventIds.ValidationRejected, "Signal validation failed: {Msg}", errMsg);
                return new ExecutionResult(intent.IntentId, string.Empty, false, errMsg, 0, 0, DateTime.UtcNow);
            }

            // PRO ARCHITECTURE: Open an isolated transactional scope safely to handle all DB repositories and scoped Execution Gateways.
            // This prevents Captive Dependency and DB context concurrency crashes.
            using var dbScope = _scopeFactory.CreateScope();
            var accountRepository = dbScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var orderRepository = dbScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var positionRepository = dbScope.ServiceProvider.GetRequiredService<IPositionRepository>();
            var unitOfWork = dbScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // REASON: Resolving the scoped ExecutionGateway Adapter dynamically inside the transaction scope
            var executionGateway = dbScope.ServiceProvider.GetRequiredService<IExecutionGateway>();

            // 3. Create domain Order entity (Initially Pending)
            var symbol = new Symbol(signal.SymbolName);
            var lotSize = new LotSize(signal.Volume);

            var order = Order.CreateNew(
                symbol,
                signal.Direction,
                signal.Type,
                lotSize,
                signal.Price,
                signal.StopLoss,
                signal.TakeProfit);

            try
            {
                // Save initial order status as pending in repository
                await orderRepository.AddAsync(order);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                _auditService.LogOrderSubmitted(order.Id, symbol.Name, corrId);

                _logger.LogStructured(LogLevel.Information, LogEventIds.OrderSubmitted, "Order submitted: OrderId={OrderId} Symbol={Symbol}", order.Id, symbol.Name);

                // 4. Retrieve Account Status
                var account = await accountRepository.GetByIdAsync("DEFAULT_ACCOUNT")
                    ?? await accountRepository.GetByIdAsync("ACC_12345");

                if (account == null)
                {
                    string errMsg = "Active broker account not found in persistence layer.";
                    order.Reject(errMsg);
                    await orderRepository.UpdateAsync(order);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
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
                    await orderRepository.UpdateAsync(order);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

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
                var report = await executionGateway.ExecuteAsync(command, cancellationToken); // Mapped dynamically from scope

                // 7. Update order status based on Gateway Execution Report
                if (report.IsSuccess)
                {
                    order.Fill(report.TicketId, report.ExecutionPrice);
                    await orderRepository.UpdateAsync(order);

                    // Create/Update Open Position
                    var positionId = Guid.NewGuid();
                    var position = new Position(
                        positionId,
                        report.TicketId,
                        symbol,
                        order.Direction,
                        order.Volume,
                        report.ExecutionPrice,
                        report.ExecutionPrice,
                        order.StopLoss,
                        order.TakeProfit
                    );
                    await positionRepository.AddAsync(position);

                    // Update account equity and free margin safely
                    decimal marginCost = (decimal)((double)report.ExecutionPrice * (double)report.ExecutedVolume * 100.0);
                    account.UpdateBalanceAndEquity(account.Balance, account.Equity, account.Margin + marginCost, account.FreeMargin - marginCost);
                    await accountRepository.UpsertAsync(account);

                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    _auditService.LogOrderExecutionResult(order.Id, report.TicketId, true, "Success", corrId);

                    _logger.LogStructured(LogLevel.Information, LogEventIds.OrderFilled, "Order successfully FILLED on Gateway. TicketId={TicketId} Price={Price}", report.TicketId, report.ExecutionPrice);

                    // Publish Execution Completed Event
                    _decisionStream.PublishExecutionCompleted(new ExecutionCompletedEvent(
                        Guid.NewGuid(),
                        symbol.Name,
                        signal.Direction.ToString(),
                        (double)report.ExecutionPrice,
                        (double)report.ExecutedVolume,
                        true,
                        string.Empty
                    ));

                    return new ExecutionResult(intent.IntentId, report.TicketId, true, string.Empty, report.ExecutionPrice, report.ExecutedVolume, report.Timestamp);
                }
                else
                {
                    order.Reject(report.ErrorMessage);
                    await orderRepository.UpdateAsync(order);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    _auditService.LogOrderExecutionResult(order.Id, string.Empty, false, report.ErrorMessage, corrId);

                    _logger.LogStructured(LogLevel.Warning, LogEventIds.OrderRejected, "Order REJECTED by Gateway: {Reason}", report.ErrorMessage);

                    // Publish Execution Failed Event
                    _decisionStream.PublishExecutionCompleted(new ExecutionCompletedEvent(
                        Guid.NewGuid(),
                        symbol.Name,
                        signal.Direction.ToString(),
                        0,
                        0,
                        false,
                        report.ErrorMessage
                    ));

                    return new ExecutionResult(intent.IntentId, string.Empty, false, report.ErrorMessage, 0, 0, report.Timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Unexpected error in Execution Pipeline.");
                order.Reject(ex.Message);
                try
                {
                    await orderRepository.UpdateAsync(order);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // Ignore DB secondary failures to retain primary exception
                }
                return new ExecutionResult(intent.IntentId, string.Empty, false, $"Pipeline Exception: {ex.Message}", 0, 0, DateTime.UtcNow);
            }
        }
        #endregion

        #region Disposal
        public void Dispose()
        {
            if (_decisionStream != null)
            {
                _decisionStream.OnDecisionCreated -= HandleDecisionCreated;
            }
        }
        #endregion
    }
}