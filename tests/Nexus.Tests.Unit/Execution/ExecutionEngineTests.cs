using Microsoft.Extensions.Logging;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Execution;
using Nexus.Execution.Auditing;
using Nexus.Execution.Domain;
using Nexus.Execution.Enums;
using Nexus.Execution.Gateways;
using Nexus.Execution.Management;
using Nexus.Execution.Risk;

namespace Nexus.Tests.Unit.Execution
{
    public class ExecutionEngineTests
    {
        #region Test Lifecycle & Setup

        private readonly SimulationExecutionGateway _simGateway;
        private readonly StubMt5TradingService _stubMt5Service;
        private readonly MT5ExecutionGateway _mt5Gateway;
        private readonly RiskExecutionGuard _riskGuard;
        private readonly StubExecutionAuditService _auditService;
        private readonly PositionManager _positionManager;
        private readonly StubLogger<RiskControlledExecutionEngine> _logger;
        private readonly RiskControlledExecutionEngine _engine;

        public ExecutionEngineTests()
        {
            _simGateway = new SimulationExecutionGateway();
            _stubMt5Service = new StubMt5TradingService();
            _mt5Gateway = new MT5ExecutionGateway(_stubMt5Service);
            _riskGuard = new RiskExecutionGuard();
            _auditService = new StubExecutionAuditService();
            _logger = new StubLogger<RiskControlledExecutionEngine>();

            _engine = new RiskControlledExecutionEngine(
                _simGateway,
                _mt5Gateway,
                _riskGuard,
                _auditService,
                _positionManager,
                _logger
            );
        }

        #endregion

        #region Order Lifecycle & State Machine Tests

        [Fact]
        public void OrderRequest_InitialState_ShouldBeCreated()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Signal Buy");
            Assert.Equal(ExecutionState.Created, req.State);
            Assert.True((DateTime.UtcNow - req.CreatedAt).TotalSeconds < 5);
        }

        [Fact]
        public void OrderRequest_TransitionTo_ShouldUpdateStateAndTimestamp()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Signal Buy");
            var initialUpdate = req.UpdatedAt;

            Thread.Sleep(5); // Force elapsed time
            req.TransitionTo(ExecutionState.Validated);

            Assert.Equal(ExecutionState.Validated, req.State);
            Assert.True(req.UpdatedAt > initialUpdate);
        }

        #endregion

        #region Risk Rejection Tests

        [Fact]
        public async Task RiskGuard_MissingStopLoss_ShouldReject()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, null, 1.1100, "No SL");
            var result = await _engine.ExecuteOrderAsync(req, 10000, 10000, 0, 0, "Normal");

            Assert.False(result.Success);
            Assert.Contains("Stop Loss is mandatory", result.Error);
            Assert.Equal(ExecutionState.Rejected, req.State);
        }

        [Fact]
        public async Task RiskGuard_ExceedingDailyLoss_ShouldReject()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Normal");
            _riskGuard.MaxDailyLossLimit = 1000.0;

            var result = await _engine.ExecuteOrderAsync(req, 10000, 10000, 0, 1200.0, "Normal");

            Assert.False(result.Success);
            Assert.Contains("daily loss limit exceeded", result.Error);
        }

        [Fact]
        public async Task RiskGuard_ExceedingSinglePositionSize_ShouldReject()
        {
            var req = new OrderRequest("EURUSD", "Buy", 12.0, 1.1000, 1.0950, 1.1100, "Too Big");
            _riskGuard.MaxPositionSize = 5.0;

            var result = await _engine.ExecuteOrderAsync(req, 10000, 10000, 0, 0, "Normal");

            Assert.False(result.Success);
            Assert.Contains("exceeds maximum allowed position size", result.Error);
        }

        [Fact]
        public async Task RiskGuard_ExceedingCumulativeExposure_ShouldReject()
        {
            var req = new OrderRequest("EURUSD", "Buy", 2.0, 1.1000, 1.0950, 1.1100, "Normal");
            _riskGuard.MaxExposureLimit = 50000.0; // max exposure is $50k

            // request has $220k exposure (2 lots * 100,000 multiplier or similar, but here exposure calculation in risk engine is Volume * Entry = 2 * 1.10 = 2.2)
            // Wait, the formula in risk guard is: additionalExposure = Volume * Entry
            // So additional exposure = 2 * 1.1000 = 2.20
            // Let's set limit to 2.0 to trigger breach
            _riskGuard.MaxExposureLimit = 2.0;

            var result = await _engine.ExecuteOrderAsync(req, 10000, 10000, 0, 0, "Normal");

            Assert.False(result.Success);
            Assert.Contains("exceeds maximum cumulative exposure", result.Error);
        }

        [Fact]
        public async Task RiskGuard_ExceedingRiskPercentageLimit_ShouldReject()
        {
            // EURUSD Buy, entry = 1.1000, sl = 1.0500. Risk price diff = 0.0500.
            // Volume = 1.0 lot. Multiplier for EURUSD = 100,000.
            // Trade risk = 0.05 * 1.0 * 100,000 = $5,000.
            // Balance / Equity = 10,000. Risk percentage = 5,000 / 10,000 = 50%. Limit is 2% (0.02).
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0500, 1.1500, "Aggressive Risk");
            var result = await _engine.ExecuteOrderAsync(req, 10000, 10000, 0, 0, "Normal");

            Assert.False(result.Success);
            Assert.Contains("exceeds maximum risk percentage", result.Error);
        }

        [Fact]
        public async Task RiskGuard_RestrictedMarketRegime_ShouldReject()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Normal");
            var result = await _engine.ExecuteOrderAsync(req, 100000, 100000, 0, 0, "ExtremeVolatility");

            Assert.False(result.Success);
            Assert.Contains("restricted market conditions", result.Error);
        }

        #endregion

        #region Simulation Execution Tests

        [Fact]
        public async Task SimulationGateway_ExecuteOrder_ShouldSucceedAndTrackPosition()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Sim Trade");
            _engine.Profile = ExecutionProfile.Simulation;

            var result = await _engine.ExecuteOrderAsync(req, 100000, 100000, 0, 0, "Normal");

            Assert.True(result.Success);
            Assert.StartsWith("SIM_", result.OrderId);
            Assert.Equal(ExecutionState.Filled, req.State);

            // Verify position was registered in position manager
            var positions = _positionManager.OpenPositions;
            Assert.Single(positions);
            Assert.Equal(result.OrderId, positions[0].TicketId);
            Assert.Equal("EURUSD", positions[0].Symbol);
            Assert.Equal("Buy", positions[0].Direction);
            Assert.Equal(1.0, positions[0].Volume);
        }

        #endregion

        #region Position Management Tests

        [Fact]
        public async Task PositionManager_HandleStopModification_ShouldModifyStops()
        {
            var initialSnapshot = new PositionSnapshot(
                ticketId: "SIM_3001",
                symbol: "EURUSD",
                direction: "Buy",
                volume: 1.0,
                entryPrice: 1.1000,
                currentPrice: 1.1000,
                stopLoss: 1.0950,
                takeProfit: 1.1100,
                unrealizedPnl: 0m,
                riskExposure: 1.10,
                status: "OPEN"
            );

            _simGateway.TrackPosition(initialSnapshot);
            _positionManager.TrackPosition(initialSnapshot);

            var result = await _positionManager.HandleStopModificationAsync("SIM_3001", 1.0980, 1.1200);

            Assert.True(result.Success);
            var openPositions = _positionManager.OpenPositions;
            Assert.Equal(1.0980, openPositions[0].StopLoss);
            Assert.Equal(1.1200, openPositions[0].TakeProfit);
        }

        [Fact]
        public async Task PositionManager_HandlePartialClose_ShouldDecreaseVolume()
        {
            var initialSnapshot = new PositionSnapshot(
                ticketId: "SIM_3002",
                symbol: "EURUSD",
                direction: "Buy",
                volume: 1.0,
                entryPrice: 1.1000,
                currentPrice: 1.1000,
                stopLoss: 1.0950,
                takeProfit: 1.1100,
                unrealizedPnl: 0m,
                riskExposure: 1.10,
                status: "OPEN"
            );

            _simGateway.TrackPosition(initialSnapshot);
            _positionManager.TrackPosition(initialSnapshot);

            // Close 0.4 lots of the 1.0 lots position
            var result = await _positionManager.HandlePartialCloseAsync("SIM_3002", 0.4, 1.1050);

            Assert.True(result.Success);

            // Open position volume should now be 0.6
            var openPositions = _positionManager.OpenPositions;
            Assert.Single(openPositions);
            Assert.Equal(0.6, openPositions[0].Volume);

            // Closed list should record the closed 0.4 portion
            var closedPositions = _positionManager.ClosedPositions;
            Assert.Single(closedPositions);
            Assert.Equal("SIM_3002_PARTIAL", closedPositions[0].TicketId);
            Assert.Equal(0.4, closedPositions[0].Volume);
        }

        [Fact]
        public async Task PositionManager_HandleFullClose_ShouldMoveToClosedBag()
        {
            var initialSnapshot = new PositionSnapshot(
                ticketId: "SIM_3003",
                symbol: "EURUSD",
                direction: "Buy",
                volume: 1.0,
                entryPrice: 1.1000,
                currentPrice: 1.1000,
                stopLoss: 1.0950,
                takeProfit: 1.1100,
                unrealizedPnl: 0m,
                riskExposure: 1.10,
                status: "OPEN"
            );

            _simGateway.TrackPosition(initialSnapshot);
            _positionManager.TrackPosition(initialSnapshot);

            var result = await _positionManager.HandlePartialCloseAsync("SIM_3003", 1.0, 1.1050);

            Assert.True(result.Success);
            Assert.Empty(_positionManager.OpenPositions);

            var closedPositions = _positionManager.ClosedPositions;
            Assert.Single(closedPositions);
            Assert.Equal("SIM_3003", closedPositions[0].TicketId);
            Assert.Equal("CLOSED", closedPositions[0].Status);
        }

        #endregion

        #region MT5 Adapter Mocking & Safe Mode Profiling Tests

        [Fact]
        public async Task LiveProfile_NoPermission_ShouldFailExecution()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Live order");
            _engine.Profile = ExecutionProfile.Live;
            _engine.IsLivePermissionGranted = false; // Disabled by default

            var result = await _engine.ExecuteOrderAsync(req, 100000, 100000, 0, 0, "Normal");

            Assert.False(result.Success);
            Assert.Contains("Live trading profile selected, but explicit Live permission", result.Error);
        }

        [Fact]
        public async Task LiveProfile_WithPermission_ShouldCallMT5ServiceAndSucceed()
        {
            var req = new OrderRequest("EURUSD", "Buy", 1.0, 1.1000, 1.0950, 1.1100, "Live order");
            _engine.Profile = ExecutionProfile.Live;
            _engine.IsLivePermissionGranted = true; // explicitly authorized

            // Configure stub MT5 service
            _stubMt5Service.NextPlaceOrderResult = new PlaceOrderResult(true, 777888, "Success", "Executed", "Live order");

            var result = await _engine.ExecuteOrderAsync(req, 100000, 100000, 0, 0, "Normal");

            Assert.True(result.Success);
            Assert.Equal("777888", result.OrderId);
            Assert.Equal(ExecutionState.Filled, req.State);
            Assert.Equal("777888", _stubMt5Service.LastSymbolPlaced == "EURUSD" ? "777888" : string.Empty);
        }

        #endregion

        #region Mock Helper Stubs

        private class StubMt5TradingService : IMt5TradingService
        {
            public PlaceOrderResult NextPlaceOrderResult { get; set; } = new PlaceOrderResult(true, 111, "Success", "", "");
            public ClosePositionResult NextClosePositionResult { get; set; } = new ClosePositionResult(true, 111, "");
            public string? LastSymbolPlaced { get; private set; }

            public Task<PlaceOrderResult> PlaceMarketOrderAsync(string symbol, BridgeOrderSide side, decimal volume, decimal? stopLoss, decimal? takeProfit, string? comment, string? clientCorrelationId, CancellationToken cancellationToken)
            {
                LastSymbolPlaced = symbol;
                return Task.FromResult(NextPlaceOrderResult);
            }

            public Task<PlaceOrderResult> ModifyPositionAsync(long positionTicket, string symbol, decimal sl, decimal tp, CancellationToken cancellationToken)
            {
                return Task.FromResult(new PlaceOrderResult(true, positionTicket, "Success", "", ""));
            }

            public Task<ClosePositionResult> ClosePositionAsync(long positionTicket, string symbol, decimal? volume, CancellationToken cancellationToken)
            {
                return Task.FromResult(NextClosePositionResult);
            }

            public Task<IReadOnlyList<OpenPositionDto>> GetOpenPositionsAsync(CancellationToken cancellationToken)
            {
                IReadOnlyList<OpenPositionDto> list = new List<OpenPositionDto>
                {
                    new OpenPositionDto(777888, "EURUSD", "Buy", 1.0m, 1.1000m, 1.1000m, 1.0950m, 1.1100m, 0m, 0m, 12345, "Live order", DateTime.UtcNow)
                };
                return Task.FromResult(list);
            }
        }

        private class StubExecutionAuditService : IExecutionAuditService
        {
            public Task RecordOrderAsync(OrderRequest request, Guid? accountId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RecordExecutionErrorAsync(string? orderId, string errorCode, string errorMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RecordPositionAsync(PositionSnapshot snapshot, Guid? accountId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private class StubLogger<T> : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
            }
        }

        #endregion
    }
}
