using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Analytics;
using Nexus.Application.Pipeline;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Application.Strategies;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Persistence.Models;
using Nexus.Tests.EndToEnd.Fixture;
using Nexus.Tests.EndToEnd.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace Nexus.Tests.EndToEnd
{
    public class E2EWorkflowTests
    {
        private readonly ITestOutputHelper _output;

        public E2EWorkflowTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        private async Task SeedAccountAsync(E2ETestHost host, string accountId = "ACC_12345", decimal balance = 10000m, decimal equity = 10000m)
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NexusDbContext>();

            var existing = await dbContext.Accounts.FirstOrDefaultAsync(a => a.BrokerAccountId == accountId);
            if (existing != null)
            {
                existing.Balance = balance;
                existing.Equity = equity;
                existing.FreeMargin = balance;
                await dbContext.SaveChangesAsync();
                return;
            }

            var accountModel = new AccountDbModel
            {
                Id = Guid.NewGuid(),
                BrokerAccountId = accountId,
                BrokerName = "E2E_TEST_BROKER",
                Currency = "USD",
                Balance = balance,
                Equity = equity,
                Margin = 0m,
                FreeMargin = balance,
                Leverage = 100,
                IsLive = false,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await dbContext.Accounts.AddAsync(accountModel);
            await dbContext.SaveChangesAsync();
        }

        private void LogFailureDiagnostics(
            string testName,
            string correlationId,
            string workflow,
            string strategyId,
            string accountId,
            string symbol,
            string orderId,
            string positionId,
            Exception ex)
        {
            _output.WriteLine("============================================================");
            _output.WriteLine($"E2E DIAGNOSTIC FAILURE REPORT - {testName}");
            _output.WriteLine("============================================================");
            _output.WriteLine($"CorrelationId: {correlationId}");
            _output.WriteLine($"Workflow: {workflow}");
            _output.WriteLine($"StrategyId: {strategyId}");
            _output.WriteLine($"AccountId: {accountId}");
            _output.WriteLine($"Symbol: {symbol}");
            _output.WriteLine($"OrderId: {orderId}");
            _output.WriteLine($"PositionId: {positionId}");
            _output.WriteLine($"Error: {ex.Message}");
            _output.WriteLine($"StackTrace: {ex.StackTrace}");
            _output.WriteLine("============================================================");
        }

        [Fact]
        public async Task Flow1_MarketDataIntakeFlow_SavesTicks_AndNotifiesStrategy()
        {
            string correlationId = "E2E_CORR_FLOW_1";
            string strategyId = "strategy_1";
            string symbol = "EURUSD";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();
                await SeedAccountAsync(host);

                // Setup Strategy
                bool strategyNotified = false;
                string capturedCorrelationId = string.Empty;
                Tick capturedTick = default;

                var mockStrategy = new MockE2EStrategy("DataIntakeStrategy", async (tick) =>
                {
                    strategyNotified = true;
                    capturedTick = tick;
                    capturedCorrelationId = correlationId;
                    await Task.CompletedTask;
                });

                var descriptor = new StrategyDescriptor(
                    strategyId,
                    "DataIntakeStrategy",
                    new List<string> { symbol },
                    new Dictionary<string, string>()
                );

                // Register with host
                var strategyHost = new StrategyHost(mockStrategy, descriptor, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);
                host.Supervisor.AddHost(strategyHost);
                await host.Supervisor.StartAllAsync();

                // Setup Feed Subscriber
                host.MarketFeed.OnTickReceived += async (envelope) =>
                {
                    // Core application market data ingestion path
                    using var scope = host.Services.CreateScope();
                    var marketRepo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();

                    var sym = new Symbol(envelope.SymbolName);
                    var tick = new Tick(sym, envelope.Timestamp, envelope.Bid, envelope.Ask);

                    // Check Validation Boundaries
                    if (InputValidator.ValidateSymbol(envelope.SymbolName) && InputValidator.ValidatePrice(envelope.Bid))
                    {
                        await marketRepo.AppendTickAsync(tick);
                        await host.Supervisor.RouteTickAsync(tick, correlationId);
                    }
                };

                // Trigger Tick Ingestion
                await host.MarketFeed.PushTickAsync(symbol, 1.08500, 1.08510);

                // Assertions
                Assert.True(strategyNotified, "The strategy should have been notified by the tick routing pipeline.");
                Assert.Equal(symbol, capturedTick.Symbol.Name);
                Assert.Equal(1.08500, capturedTick.Bid);

                // Verify db persistence
                using (var scope = host.Services.CreateScope())
                {
                    var marketRepo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();
                    var ticksList = new List<Tick>();
                    await foreach (var t in marketRepo.StreamTicksAsync(new Symbol(symbol), DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5)))
                    {
                        ticksList.Add(t);
                    }
                    Assert.NotEmpty(ticksList);
                    Assert.Contains(ticksList, t => t.Bid == 1.08500);
                }

                // Clean up
                await host.Supervisor.StopAllAsync();
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow1_MarketDataIntakeFlow", correlationId, "MarketDataIngestion", strategyId, "ACC_12345", symbol, "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow2_StrategySignalToOrderFlow_CoordinatesExecution_AndPersistsState()
        {
            string correlationId = "E2E_SIGNAL_CORR";
            string strategyId = "strategy_2";
            string symbol = "EURUSD";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();
                await SeedAccountAsync(host);

                using var scope = host.Services.CreateScope();
                var coordinator = scope.ServiceProvider.GetRequiredService<ExecutionCoordinator>();
                var auditService = scope.ServiceProvider.GetRequiredService<ExecutionAuditService>();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                // Trigger Signal
                var signal = new TradeSignal(
                    StrategyId: strategyId,
                    SymbolName: symbol,
                    Direction: OrderDirection.Buy,
                    Type: OrderType.Market,
                    Volume: 1.0,
                    Price: 1.08500
                );

                var context = new PipelineContext(signal.StrategyId, correlationId);
                var result = await coordinator.ProcessSignalAsync(signal, context);

                // Assert execution outcome
                Assert.True(result.IsSuccess);
                Assert.NotEmpty(result.TicketId);

                // Verify order saved in DB
                var openOrders = await orderRepo.GetOpenOrdersAsync();
                var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
                var savedOrder = await db.Orders.FirstOrDefaultAsync(o => o.StatusReason == string.Empty);
                Assert.NotNull(savedOrder);
                Assert.Equal("Filled", savedOrder.Status);

                // Verify audit trace
                var logs = auditService.GetAuditTrail();
                Assert.Contains(logs, l => l.Contains(correlationId) && l.Contains("Signal received"));
                Assert.Contains(logs, l => l.Contains(correlationId) && l.Contains("Order") && l.Contains("execution outcome"));
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow2_StrategySignalToOrderFlow", correlationId, "SignalExecution", strategyId, "ACC_12345", symbol, "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow3_PositionLifecycleFlow_UpdatesPnL_AndTriggersTrailingStop()
        {
            string correlationId = "E2E_SIGNAL_FLOW_3";
            string strategyId = "strategy_3";
            string symbol = "EURUSD";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();
                await SeedAccountAsync(host);

                using var scope = host.Services.CreateScope();
                var coordinator = scope.ServiceProvider.GetRequiredService<ExecutionCoordinator>();
                var posRepo = scope.ServiceProvider.GetRequiredService<IPositionRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // 1. Submit signal to open position
                var signal = new TradeSignal(strategyId, symbol, OrderDirection.Buy, OrderType.Market, 1.0, 1.08500, StopLoss: 1.08400, TakeProfit: 1.08700);
                var result = await coordinator.ProcessSignalAsync(signal, new PipelineContext(strategyId, correlationId));
                Assert.True(result.IsSuccess);

                // 2. Verify position is open
                var openPosList = await posRepo.GetOpenPositionsAsync();
                var position = openPosList.FirstOrDefault(p => p.Symbol.Name == symbol);
                Assert.NotNull(position);
                Assert.Equal(1.08500, position.EntryPrice);
                Assert.Equal((decimal)0.0, position.UnrealizedPnl); // Initial state at same price

                // 3. Move price in favorable direction to verify Unrealized PnL updates
                position.UpdatePrice(1.08600);
                await posRepo.UpdateAsync(position);
                await unitOfWork.SaveChangesAsync();

                // PnL: (1.08600 - 1.08500) * 1.0 lot * 100000 multiplier = 0.001 * 100000 = +$100.00
                Assert.Equal(100m, position.UnrealizedPnl);

                // 4. Simulate Trailing Stop Adjustment trigger
                if (position.CurrentPrice - position.EntryPrice >= 0.0010)
                {
                    position.ModifySlTp(1.08500, position.TakeProfit);
                    await posRepo.UpdateAsync(position);
                    await unitOfWork.SaveChangesAsync();
                }

                // Retrieve from DB to verify persistence of lifecycle state
                var savedPos = await posRepo.GetByIdAsync(position.Id.ToString());
                Assert.NotNull(savedPos);
                Assert.Equal(1.08500, savedPos.StopLoss);
                Assert.Equal(100m, savedPos.UnrealizedPnl);
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow3_PositionLifecycleFlow", correlationId, "PositionLifecycle", strategyId, "ACC_12345", symbol, "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow4_RiskRejectionFlow_BlocksUnauthorizedOrders()
        {
            string correlationId = "CORR_REJECT_DRAWDOWN";
            string strategyId = "strategy_4";
            string symbol = "EURUSD";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();

                // Seed account with high drawdown to trigger Risk Rejection
                await SeedAccountAsync(host, "ACC_12345", balance: 10000m, equity: 7000m); // 30% drawdown, exceeds limit of 20%

                using var scope = host.Services.CreateScope();
                var coordinator = scope.ServiceProvider.GetRequiredService<ExecutionCoordinator>();
                var auditService = scope.ServiceProvider.GetRequiredService<ExecutionAuditService>();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                var signal = new TradeSignal(strategyId, symbol, OrderDirection.Buy, OrderType.Market, 1.0, 1.08500);
                var result = await coordinator.ProcessSignalAsync(signal, new PipelineContext(strategyId, correlationId));

                // Verification
                Assert.False(result.IsSuccess);
                Assert.Contains("Risk Rejected", result.ErrorMessage);

                // Assert NO gateway dispatch execution happened (TicketId is empty)
                Assert.Empty(result.TicketId);

                // Verify order status recorded as Rejected
                var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
                var rejectedOrder = await db.Orders.FirstOrDefaultAsync(o => o.StatusReason.Contains("exceeds maximum limit"));
                Assert.NotNull(rejectedOrder);
                Assert.Equal("Rejected", rejectedOrder.Status);

                // Verify Audit Log generated
                var logs = auditService.GetAuditTrail();
                Assert.Contains(logs, l => l.Contains(correlationId) && l.Contains("Risk check REJECTED"));
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow4_RiskRejectionFlow", correlationId, "SignalExecution", strategyId, "ACC_12345", symbol, "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow5a_RecoveryAndRestartFlow_RestoresTradingStateCorrectly()
        {
            string correlationId = "E2E_RECOVERY_FLOW_5A";
            string symbolStr = "XAUUSD";
            Guid orderId = Guid.NewGuid();
            Guid positionId = Guid.NewGuid();

            try
            {
                _output.WriteLine("[E2E] [RecoveryStart] Starting recovery flow test 5a...");

                Testcontainers.PostgreSql.PostgreSqlContainer? sharedContainer = null;
                bool isDockerAvailable = false;

                {
                    // STEP 1: Process partial workflow and save to DB
                    // We set ownsContainer: false so the PostgreSQL container remains running upon host1 disposal
                    await using var host1 = new E2ETestHost(ownsContainer: false, outputHelper: _output);
                    await host1.InitializeAsync();
                    sharedContainer = host1.PostgresContainer;
                    isDockerAvailable = host1.IsDockerAvailable;

                    await SeedAccountAsync(host1);

                    using var scope1 = host1.Services.CreateScope();
                    var dbContext1 = scope1.ServiceProvider.GetRequiredService<NexusDbContext>();

                    // Simulate filled Order
                    var orderDb = new OrderDbModel
                    {
                        Id = orderId,
                        TicketId = "TKT_RECOVER_999",
                        Symbol = symbolStr,
                        Direction = "Buy",
                        Type = "Market",
                        Volume = 1.0m,
                        Price = 2030.50,
                        StopLoss = 2020.0,
                        TakeProfit = 2050.0,
                        Status = "Filled",
                        StatusReason = string.Empty,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    // Simulate Position open state
                    var positionDb = new PositionDbModel
                    {
                        Id = positionId,
                        TicketId = "TKT_RECOVER_999",
                        Symbol = symbolStr,
                        Direction = "Buy",
                        Volume = 1.0m,
                        EntryPrice = 2030.50,
                        CurrentPrice = 2035.50,
                        StopLoss = 2020.0,
                        TakeProfit = 2050.0,
                        UnrealizedPnl = 500.0m,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    await dbContext1.Orders.AddAsync(orderDb);
                    await dbContext1.Positions.AddAsync(positionDb);
                    await dbContext1.SaveChangesAsync();

                    _output.WriteLine($"[E2E] [StateSnapshotLoaded] Host1 saved initial states. OrderId={orderId}, PositionId={positionId}");
                }

                // STEP 2: Recreate host, reload state and continue
                // We pass the running container and set ownsContainer: true so host2 will clean it up on disposal
                await using var host2 = new E2ETestHost(isDockerAvailable ? sharedContainer : null, ownsContainer: true, outputHelper: _output);
                await host2.InitializeAsync();

                using (var scope2 = host2.Services.CreateScope())
                {
                    var posRepo = scope2.ServiceProvider.GetRequiredService<IPositionRepository>();
                    var orderRepo = scope2.ServiceProvider.GetRequiredService<IOrderRepository>();

                    // Verify states reloaded correctly
                    var pos = await posRepo.GetByIdAsync(positionId.ToString());
                    Assert.NotNull(pos);
                    Assert.Equal("TKT_RECOVER_999", pos.TicketId);
                    Assert.Equal(2035.50, pos.CurrentPrice);
                    Assert.Equal(500.0m, pos.UnrealizedPnl);

                    var ord = await orderRepo.GetByIdAsync(orderId.ToString());
                    Assert.NotNull(ord);
                    Assert.Equal("TKT_RECOVER_999", ord.TicketId);
                    Assert.Equal(OrderStatus.Filled, ord.Status);

                    _output.WriteLine("[E2E] [TradingStateRestored] Host2 successfully loaded persistent states from DB.");

                    // Verify recovery boundaries:
                    // What must survive: database entity states (Account, Positions, Orders)
                    Assert.Equal("XAUUSD", pos.Symbol.Name);
                    Assert.Equal("XAUUSD", ord.Symbol.Name);

                    // What is intentionally ephemeral: the StrategySupervisor's active memory hosts (starts empty)
                    Assert.Empty(host2.Supervisor.Hosts);
                    Assert.False(host2.Supervisor.IsEngineRunning);

                    _output.WriteLine("[E2E] [RuntimeRehydrationBoundaryEvaluated] Verified that transient strategy host memories are ephemeral.");

                    _output.WriteLine("[E2E] [RecoveryCompleted] Recovery boundaries successfully validated for trading state.");
                }
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow5a_RecoveryAndRestartFlow_RestoresTradingStateCorrectly", correlationId, "SystemRecovery", "", "ACC_12345", symbolStr, orderId.ToString(), positionId.ToString(), ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow5b_RecoveryAndRestartFlow_StrategyRuntimeRehydrationBoundary()
        {
            string correlationId = "E2E_RECOVERY_FLOW_5B";
            string strategyId = "strat_rehydrate_test";
            string symbolStr = "XAUUSD";

            try
            {
                _output.WriteLine("[E2E] [RecoveryStart] Starting strategy runtime rehydration boundary test 5b...");

                var customState = new Dictionary<string, string>
                {
                    { "LastSignalPrice", "2030.50" },
                    { "CumulativeVolume", "15.5" },
                    { "TradingActive", "true" }
                };

                InMemoryStrategyStateStore? sharedStateStore = null;

                // STEP 1: Save strategy state in state store
                {
                    await using var host1 = new E2ETestHost(outputHelper: _output);
                    await host1.InitializeAsync();
                    sharedStateStore = host1.StateStore;

                    var stateStore = host1.Services.GetRequiredService<IStrategyStateStore>();
                    await stateStore.SaveStateAsync(strategyId, customState);

                    _output.WriteLine($"[E2E] [StateSnapshotLoaded] Host1 saved strategy state to store for StrategyId={strategyId}");
                } // Host1 disposed here

                // STEP 2: Recreate host, reload state store
                {
                    await using var host2 = new E2ETestHost(stateStore: sharedStateStore, outputHelper: _output);
                    await host2.InitializeAsync();

                    var stateStore = host2.Services.GetRequiredService<IStrategyStateStore>();

                    var loadedState = await stateStore.LoadStateAsync(strategyId);
                    Assert.NotNull(loadedState);
                    Assert.Equal("2030.50", loadedState["LastSignalPrice"]);
                    Assert.Equal("15.5", loadedState["CumulativeVolume"]);
                    Assert.Equal("true", loadedState["TradingActive"]);

                    _output.WriteLine("[E2E] [TradingStateRestored] Host2 successfully loaded strategy state dictionary from state store.");

                    // Prove Ephemeral boundaries: StrategyHost instances are ephemeral by default
                    var mockStrategy = new MockE2EStrategy("RehydrateStrategy");
                    var descriptor = new StrategyDescriptor(strategyId, "RehydrateStrategy", new List<string> { symbolStr }, new Dictionary<string, string>());
                    var hostInstance = new StrategyHost(mockStrategy, descriptor, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

                    // Initially, hostInstance is not running and has empty internal strategy memory
                    Assert.False(hostInstance.IsRunning);

                    _output.WriteLine("[E2E] [RuntimeRehydrationBoundaryEvaluated] Verified new StrategyHost instance starts uninitialized/ephemeral.");

                    // Strategy can be started and manually rehydrated from state store state
                    await hostInstance.InitializeAsync();
                    await hostInstance.StartAsync();
                    Assert.True(hostInstance.IsRunning);

                    _output.WriteLine("[E2E] [RecoveryCompleted] Recovery boundaries successfully validated for strategy runtime rehydration.");
                }
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow5b_RecoveryAndRestartFlow_StrategyRuntimeRehydrationBoundary", correlationId, "SystemRecovery", strategyId, "ACC_12345", symbolStr, "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow6_MultiStrategyConcurrencyFlow_IsolatesExecutionStates()
        {
            string correlationId = "E2E_CORR_FLOW_6";
            string strategyId1 = "strat_1";
            string strategyId2 = "strat_2";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();
                await SeedAccountAsync(host);

                var ticks1 = new List<Tick>();
                var ticks2 = new List<Tick>();

                // Setup Strategy 1 (Subscribes to EURUSD)
                var strategy1 = new MockE2EStrategy("GoldMiner", async (t) =>
                {
                    ticks1.Add(t);
                    await Task.CompletedTask;
                });
                var desc1 = new StrategyDescriptor(strategyId1, "GoldMiner", new List<string> { "EURUSD" }, new Dictionary<string, string>());
                var host1 = new StrategyHost(strategy1, desc1, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

                // Setup Strategy 2 (Subscribes to GBPUSD)
                var strategy2 = new MockE2EStrategy("TrendFollower", async (t) =>
                {
                    ticks2.Add(t);
                    await Task.CompletedTask;
                });
                var desc2 = new StrategyDescriptor(strategyId2, "TrendFollower", new List<string> { "GBPUSD" }, new Dictionary<string, string>());
                var host2 = new StrategyHost(strategy2, desc2, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

                host.Supervisor.AddHost(host1);
                host.Supervisor.AddHost(host2);
                await host.Supervisor.StartAllAsync();

                // Feed ticks
                var eurusd = new Symbol("EURUSD");
                var gbpusd = new Symbol("GBPUSD");

                await host.Supervisor.RouteTickAsync(new Tick(eurusd, DateTime.UtcNow, 1.08500, 1.08510), "CORR_1");
                await host.Supervisor.RouteTickAsync(new Tick(gbpusd, DateTime.UtcNow, 1.27200, 1.27210), "CORR_2");

                // Verify separate isolated states
                Assert.Single(ticks1);
                Assert.Equal("EURUSD", ticks1[0].Symbol.Name);

                Assert.Single(ticks2);
                Assert.Equal("GBPUSD", ticks2[0].Symbol.Name);

                await host.Supervisor.StopAllAsync();
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow6_MultiStrategyConcurrencyFlow", correlationId, "StrategyDispatch", $"{strategyId1},{strategyId2}", "ACC_12345", "EURUSD,GBPUSD", "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task Flow7_LargeBatchPerformanceSanityFlow_ProcessesUnderBroadBound()
        {
            string correlationId = "E2E_CORR_FLOW_7";
            string symbol = "EURUSD";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();
                await SeedAccountAsync(host);

                using var scope = host.Services.CreateScope();
                var marketRepo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();

                int batchSize = 1000;
                var sym = new Symbol(symbol);
                var ticks = new List<Tick>(batchSize);
                var baseTime = DateTime.UtcNow;

                for (int i = 0; i < batchSize; i++)
                {
                    ticks.Add(new Tick(sym, baseTime.AddMilliseconds(i), 1.08500 + (i * 0.00001), 1.08510 + (i * 0.00001)));
                }

                var start = DateTime.UtcNow;

                // Batch append
                await marketRepo.AppendTicksAsync(ticks);

                var duration = DateTime.UtcNow - start;

                // Broad sanity check
                Assert.True(duration.TotalSeconds < 5.0, $"1000 ticks persistence took {duration.TotalSeconds}s, exceeding CI limit of 5s.");

                // Verify successful completion count
                var retrieved = new List<Tick>();
                await foreach (var t in marketRepo.StreamTicksAsync(sym, baseTime.AddSeconds(-1), baseTime.AddSeconds(5)))
                {
                    retrieved.Add(t);
                }
                Assert.Equal(batchSize, retrieved.Count);
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("Flow7_LargeBatchPerformanceSanityFlow", correlationId, "MarketDataIngestion", "", "ACC_12345", symbol, "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task E2E_GracefulNativeFallback_ExecutesSuccessfully()
        {
            string correlationId = "E2E_CORR_FLOW_8";

            try
            {
                // Simulate missing native library (IsAvailable = false)
                var fakeNative = new FakeUnavailableNativeEngine();
                var managedFallback = new ManagedIndicatorEngine();
                var indicatorEngine = new NativeIndicatorEngine(fakeNative, managedFallback);

                // Verify calculation works and uses fallback transparently
                double[] values = { 10.0, 11.0, 12.0 };
                var result = await indicatorEngine.CalculateEmaAsync(values, 2);

                Assert.Equal(3, result.Length);
                Assert.Equal(10.0, result[0], 5);
                Assert.Equal(11.0 * (2.0 / 3.0) + 10.0 * (1.0 / 3.0), result[1], 5);
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("E2E_GracefulNativeFallback", correlationId, "IndicatorCalculation", "", "", "", "", "", ex);
                throw;
            }
        }

        [Fact]
        public async Task E2E_StrategyFaultContainment_DoesNotCrashHost()
        {
            string correlationId = "E2E_FAULT_TEST";
            string strategyId1 = "strat_fail";
            string strategyId2 = "strat_healthy";
            string symbol = "EURUSD";

            try
            {
                await using var host = new E2ETestHost(outputHelper: _output);
                await host.InitializeAsync();
                await SeedAccountAsync(host);

                // Strategy 1: Failing Strategy
                var strat1 = new MockE2EStrategy("FailingStrategy");
                strat1.ConfigureFault(true);
                var desc1 = new StrategyDescriptor(strategyId1, "FailingStrategy", new List<string> { symbol }, new Dictionary<string, string>());
                var host1 = new StrategyHost(strat1, desc1, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

                // Strategy 2: Healthy Strategy
                bool healthyCalled = false;
                var strat2 = new MockE2EStrategy("HealthyStrategy", async (t) =>
                {
                    healthyCalled = true;
                    await Task.CompletedTask;
                });
                var desc2 = new StrategyDescriptor(strategyId2, "HealthyStrategy", new List<string> { symbol }, new Dictionary<string, string>());
                var host2 = new StrategyHost(strat2, desc2, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

                host.Supervisor.AddHost(host1);
                host.Supervisor.AddHost(host2);
                await host.Supervisor.StartAllAsync();

                // Route Tick
                var sym = new Symbol(symbol);
                var tick = new Tick(sym, DateTime.UtcNow, 1.08500, 1.08510);

                await host.Supervisor.RouteTickAsync(tick, correlationId);

                // Assertions
                Assert.True(healthyCalled, "The healthy strategy should execute successfully even if the other one throws an exception.");
                Assert.True(host1.IsRunning, "The failing strategy's host should remain alive after fault containment.");
                Assert.True(host2.IsRunning, "The healthy strategy's host should remain alive and fully running.");

                await host.Supervisor.StopAllAsync();
            }
            catch (Exception ex)
            {
                LogFailureDiagnostics("E2E_StrategyFaultContainment", correlationId, "StrategyDispatch", $"{strategyId1},{strategyId2}", "ACC_12345", symbol, "", "", ex);
                throw;
            }
        }

        private class FakeUnavailableNativeEngine : INativeAnalyticsEngine
        {
            public bool IsAvailable => false;
            public int CalculateEma(double[] values, int count, int period, double[] outEma) => -1;
        }
    }
}
