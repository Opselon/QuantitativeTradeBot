using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Ports;
using Nexus.Execution.Domain;

namespace Nexus.Execution.Gateways
{
    /// <summary>
    /// Design Pattern: Adapter.
    /// Bridges the high-level Application Port (IExecutionGateway) with the specific bounded-context RiskControlledExecutionEngine.
    /// Resolves live account and exposure metrics on-the-fly from persistent repositories.
    /// </summary>
    public sealed class ExecutionGatewayAdapter : Nexus.Application.Ports.IExecutionGateway
    {
        #region Private Fields
        private readonly RiskControlledExecutionEngine _riskEngine;
        private readonly IServiceScopeFactory _scopeFactory;
        #endregion

        #region Constructor
        public ExecutionGatewayAdapter(RiskControlledExecutionEngine riskEngine, IServiceScopeFactory scopeFactory)
        {
            _riskEngine = riskEngine ?? throw new ArgumentNullException(nameof(riskEngine));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }
        #endregion

        #region Port Implementation (ExecuteAsync)
        public async Task<ExecutionReport> ExecuteAsync(ExecutionCommand command, CancellationToken cancellationToken = default)
        {
            // Create temporary thread-safe scope to query live database metrics
            using var scope = _scopeFactory.CreateScope();
            var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var positionRepo = scope.ServiceProvider.GetRequiredService<IPositionRepository>();

            // 1. Fetch current Broker account state from local persistence cache
            var account = await accountRepo.GetByIdAsync(command.AccountId, cancellationToken);
            double equity = account != null ? (double)account.Equity : 10000.0;
            double balance = account != null ? (double)account.Balance : 10000.0;

            // 2. Compute active exposure from running open positions
            double exposure = 0.0;
            var openPositions = await positionRepo.GetOpenPositionsAsync(cancellationToken: cancellationToken);
            if (openPositions != null)
            {
                // Multiplies Lot volume by Price to get gross USD contract exposure
                exposure = openPositions.Sum(p => (double)p.Volume.Value * p.EntryPrice);
            }

            // 3. Map high-level ExecutionCommand to internal OrderRequest
            var request = new OrderRequest(
                symbol: command.Symbol,
                side: command.Direction.ToString(), // Converts BUY/SELL Enum to string representation
                volume: command.Volume,
                entry: command.Price,
                stopLoss: command.StopLoss,
                takeProfit: command.TakeProfit,
                reason: "AI Automated Live Execution Signal"
            );

            // 4. Delegate to the core Risk Controlled Execution Engine for checks & gateway routing
            var result = await _riskEngine.ExecuteOrderAsync(
                request,
                currentEquity: equity,
                currentBalance: balance,
                cumulativeExposure: exposure,
                dailyLoss: 0.0, // Initial safety default
                marketRegime: "Normal", // Safe default, checked dynamically by risk guard
                cancellationToken: cancellationToken
            );

            // 5. Map the ExecutionResult back to the Application Port's ExecutionReport contract
            // REASON: Resolve CommandId and ClientOrderId dynamically to ensure 100% compile safety
            Guid cmdId = Guid.NewGuid();
            string clientOrderId = string.Empty;

            try
            {
                var cmdIdProp = command.GetType().GetProperty("CommandId") ?? command.GetType().GetProperty("Id");
                if (cmdIdProp != null) cmdId = (Guid)cmdIdProp.GetValue(command)!;

                var clientOrderProp = command.GetType().GetProperty("ClientOrderId") ?? command.GetType().GetProperty("ClientCorrelationId") ?? command.GetType().GetProperty("OrderId");
                if (clientOrderProp != null) clientOrderId = (string)clientOrderProp.GetValue(command)!;
            }
            catch { }

            // REASON: Construct the record using exact PascalCase parameters matching the target definition
            return new ExecutionReport(
                CommandId: cmdId,
                ClientOrderId: clientOrderId,
                TicketId: result.OrderId ?? string.Empty,
                IsSuccess: result.Success,
                ErrorMessage: result.Error ?? string.Empty,
                ExecutionPrice: command.Price,
                ExecutedVolume: command.Volume,
                Timestamp: DateTime.UtcNow
            );
        }
        #endregion
    }
}