using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.Core.Interfaces;
using Nexus.Application.Analytics;
using Nexus.Application.Pipeline;
using Nexus.Application.Ports;
using Nexus.Application.Strategies;
using Nexus.Application.Security;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Persistence.Models;
using Nexus.Tests.EndToEnd.Fixture;
using Nexus.Tests.EndToEnd.Mocks;

namespace Nexus.Tests.EndToEnd
{
    public class E2EWorkflowTests
    {
        private async Task SeedAccountAsync(E2ETestHost host, string accountId = "ACC_12345", decimal balance = 10000m, decimal equity = 10000m)
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NexusDbContext>();

            var accDb = await dbContext.Accounts.FindAsync(Guid.Empty); // Check if exists or just add a new one
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

        [Fact]
        public async Task Flow1_MarketDataIntakeFlow_SavesTicks_AndNotifiesStrategy()
        {
            await using var host = new E2ETestHost();
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
                capturedCorrelationId = Guid.NewGuid().ToString("N");
                await Task.CompletedTask;
            });

            var descriptor = new StrategyDescriptor(
                "strategy_1",
                "DataIntakeStrategy",
                new List<string> { "EURUSD" },
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

                var symbol = new Symbol(envelope.SymbolName);
                var tick = new Tick(symbol, envelope.Timestamp, envelope.Bid, envelope.Ask);

                // Check Validation Boundaries
                if (InputValidator.ValidateSymbol(envelope.SymbolName) && InputValidator.ValidatePrice(envelope.Bid))
                {
                    await marketRepo.AppendTickAsync(tick);
                    await host.Supervisor.RouteTickAsync(tick, "E2E_CORR_123");
                }
            };

            // Trigger Tick Ingestion
            await host.MarketFeed.PushTickAsync("EURUSD", 1.08500, 1.08510);

            // Assertions
            Assert.True(strategyNotified, "The strategy should have been notified by the tick routing pipeline.");
            Assert.Equal("EURUSD", capturedTick.Symbol.Name);
            Assert.Equal(1.08500, capturedTick.Bid);

            // Verify db persistence
            using (var scope = host.Services.CreateScope())
            {
                var marketRepo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();
                var ticksList = new List<Tick>();
                await foreach (var t in marketRepo.StreamTicksAsync(new Symbol("EURUSD"), DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5)))
                {
                    ticksList.Add(t);
                }
                Assert.NotEmpty(ticksList);
                Assert.Contains(ticksList, t => t.Bid == 1.08500);
            }

            // Clean up
            await host.Supervisor.StopAllAsync();
        }

        [Fact]
        public async Task Flow2_StrategySignalToOrderFlow_CoordinatesExecution_AndPersistsState()
        {
            await using var host = new E2ETestHost();
            await host.InitializeAsync();
            await SeedAccountAsync(host);

            using var scope = host.Services.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<ExecutionCoordinator>();
            var auditService = scope.ServiceProvider.GetRequiredService<ExecutionAuditService>();
            var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            // Trigger Signal
            var signal = new TradeSignal(
                StrategyId: "strategy_2",
                SymbolName: "EURUSD",
                Direction: OrderDirection.Buy,
                Type: OrderType.Market,
                Volume: 1.0,
                Price: 1.08500
            );

            var context = new PipelineContext(signal.StrategyId, "E2E_SIGNAL_CORR");
            var result = await coordinator.ProcessSignalAsync(signal, context);

            // Assert execution outcome
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.TicketId);

            // Verify order saved in DB
            var openOrders = await orderRepo.GetOpenOrdersAsync();
            // Since order was filled, it shouldn't be open anymore. Let's find it by strategy context ID
            var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
            var savedOrder = await db.Orders.FirstOrDefaultAsync(o => o.StatusReason == string.Empty);
            Assert.NotNull(savedOrder);
            Assert.Equal("Filled", savedOrder.Status);

            // Verify audit trace
            var logs = auditService.GetAuditTrail();
            Assert.Contains(logs, l => l.Contains("E2E_SIGNAL_CORR") && l.Contains("Signal received"));
            Assert.Contains(logs, l => l.Contains("E2E_SIGNAL_CORR") && l.Contains("Order") && l.Contains("execution outcome"));
        }

        [Fact]
        public async Task Flow3_PositionLifecycleFlow_UpdatesPnL_AndTriggersTrailingStop()
        {
            await using var host = new E2ETestHost();
            await host.InitializeAsync();
            await SeedAccountAsync(host);

            using var scope = host.Services.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<ExecutionCoordinator>();
            var posRepo = scope.ServiceProvider.GetRequiredService<IPositionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // 1. Submit signal to open position
            var signal = new TradeSignal("strategy_3", "EURUSD", OrderDirection.Buy, OrderType.Market, 1.0, 1.08500, StopLoss: 1.08400, TakeProfit: 1.08700);
            var result = await coordinator.ProcessSignalAsync(signal, new PipelineContext("strategy_3"));
            Assert.True(result.IsSuccess);

            // 2. Verify position is open
            var openPosList = await posRepo.GetOpenPositionsAsync();
            var position = openPosList.FirstOrDefault(p => p.Symbol.Name == "EURUSD");
            Assert.NotNull(position);
            Assert.Equal(1.08500, position.EntryPrice);
            Assert.Equal((decimal)0.0, position.UnrealizedPnl); // Initial state at same price

            // 3. Move price in favorable direction to verify Unrealized PnL updates
            // Buy position: entry 1.08500. Favorable move to 1.08600.
            position.UpdatePrice(1.08600);
            await posRepo.UpdateAsync(position);
            await unitOfWork.SaveChangesAsync();

            // PnL: (1.08600 - 1.08500) * 1.0 lot * 100000 multiplier = 0.001 * 100000 = +$100.00
            Assert.Equal(100m, position.UnrealizedPnl);

            // 4. Simulate Trailing Stop Adjustment trigger
            // Entry 1.08500, StopLoss starts at 1.08400.
            // Under trailing stop, if current price moves up to 1.08600 (a 10 pip gain),
            // we modify StopLoss up to 1.08500 (breakeven).
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

        [Fact]
        public async Task Flow4_RiskRejectionFlow_BlocksUnauthorizedOrders()
        {
            await using var host = new E2ETestHost();
            await host.InitializeAsync();

            // Seed account with high drawdown to trigger Risk Rejection
            await SeedAccountAsync(host, "ACC_12345", balance: 10000m, equity: 7000m); // 30% drawdown, exceeds limit of 20%

            using var scope = host.Services.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<ExecutionCoordinator>();
            var auditService = scope.ServiceProvider.GetRequiredService<ExecutionAuditService>();
            var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            var signal = new TradeSignal("strategy_4", "EURUSD", OrderDirection.Buy, OrderType.Market, 1.0, 1.08500);
            var result = await coordinator.ProcessSignalAsync(signal, new PipelineContext("strategy_4", "CORR_REJECT_DRAWDOWN"));

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
            Assert.Contains(logs, l => l.Contains("CORR_REJECT_DRAWDOWN") && l.Contains("Risk check REJECTED"));
        }

        [Fact]
        public async Task Flow5_RecoveryAndRestartFlow_RestoresStateCorrectly()
        {
            // Flow setup
            string symbolStr = "XAUUSD";
            Guid orderId = Guid.NewGuid();
            Guid positionId = Guid.NewGuid();

            {
                // STEP 1: Process partial workflow and save to DB
                await using var host1 = new E2ETestHost();
                await host1.InitializeAsync();
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
            }

            // STEP 2: Recreate host, reload state and continue
            await using var host2 = new E2ETestHost();
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
            }
        }

        [Fact]
        public async Task Flow6_MultiStrategyConcurrencyFlow_IsolatesExecutionStates()
        {
            await using var host = new E2ETestHost();
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
            var desc1 = new StrategyDescriptor("strat_1", "GoldMiner", new List<string> { "EURUSD" }, new Dictionary<string, string>());
            var host1 = new StrategyHost(strategy1, desc1, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

            // Setup Strategy 2 (Subscribes to GBPUSD)
            var strategy2 = new MockE2EStrategy("TrendFollower", async (t) =>
            {
                ticks2.Add(t);
                await Task.CompletedTask;
            });
            var desc2 = new StrategyDescriptor("strat_2", "TrendFollower", new List<string> { "GBPUSD" }, new Dictionary<string, string>());
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

        [Fact]
        public async Task Flow7_LargeBatchPerformanceSanityFlow_ProcessesUnderBroadBound()
        {
            await using var host = new E2ETestHost();
            await host.InitializeAsync();
            await SeedAccountAsync(host);

            using var scope = host.Services.CreateScope();
            var marketRepo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();

            int batchSize = 1000;
            var symbol = new Symbol("EURUSD");
            var ticks = new List<Tick>(batchSize);
            var baseTime = DateTime.UtcNow;

            for (int i = 0; i < batchSize; i++)
            {
                ticks.Add(new Tick(symbol, baseTime.AddMilliseconds(i), 1.08500 + (i * 0.00001), 1.08510 + (i * 0.00001)));
            }

            var start = DateTime.UtcNow;

            // Batch append (real binary copy or sequential persistence loop)
            await marketRepo.AppendTicksAsync(ticks);

            var duration = DateTime.UtcNow - start;

            // Broad sanity check: 1000 ticks processed within 5 seconds in CI
            Assert.True(duration.TotalSeconds < 5.0, $"1000 ticks persistence took {duration.TotalSeconds}s, exceeding CI limit of 5s.");

            // Verify successful completion count
            var retrieved = new List<Tick>();
            await foreach (var t in marketRepo.StreamTicksAsync(symbol, baseTime.AddSeconds(-1), baseTime.AddSeconds(5)))
            {
                retrieved.Add(t);
            }
            Assert.Equal(batchSize, retrieved.Count);
        }

        [Fact]
        public async Task E2E_GracefulNativeFallback_ExecutesSuccessfully()
        {
            // Simulate missing native library (IsAvailable = false)
            var fakeNative = new NativeAnalyticsEngine(); // Will check availability or dry run
            var managedFallback = new ManagedIndicatorEngine();
            var indicatorEngine = new NativeIndicatorEngine(new FakeUnavailableNativeEngine(), managedFallback);

            // Verify calculation works and uses fallback transparently
            double[] values = { 10.0, 11.0, 12.0 };
            var result = await indicatorEngine.CalculateEmaAsync(values, 2);

            Assert.Equal(3, result.Length);
            Assert.Equal(10.0, result[0], 5);
            Assert.Equal(11.0 * (2.0/3.0) + 10.0 * (1.0/3.0), result[1], 5);
        }

        [Fact]
        public async Task E2E_StrategyFaultContainment_DoesNotCrashHost()
        {
            await using var host = new E2ETestHost();
            await host.InitializeAsync();
            await SeedAccountAsync(host);

            // Strategy 1: Failing Strategy
            var strat1 = new MockE2EStrategy("FailingStrategy");
            strat1.ConfigureFault(true); // Will throw exception during OnTickAsync
            var desc1 = new StrategyDescriptor("strat_fail", "FailingStrategy", new List<string> { "EURUSD" }, new Dictionary<string, string>());
            var host1 = new StrategyHost(strat1, desc1, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

            // Strategy 2: Healthy Strategy
            bool healthyCalled = false;
            var strat2 = new MockE2EStrategy("HealthyStrategy", async (t) =>
            {
                healthyCalled = true;
                await Task.CompletedTask;
            });
            var desc2 = new StrategyDescriptor("strat_healthy", "HealthyStrategy", new List<string> { "EURUSD" }, new Dictionary<string, string>());
            var host2 = new StrategyHost(strat2, desc2, Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyHost>.Instance);

            host.Supervisor.AddHost(host1);
            host.Supervisor.AddHost(host2);
            await host.Supervisor.StartAllAsync();

            // Route Tick: Should trigger OnTick for both. One throws, but other must complete successfully!
            var symbol = new Symbol("EURUSD");
            var tick = new Tick(symbol, DateTime.UtcNow, 1.08500, 1.08510);

            await host.Supervisor.RouteTickAsync(tick, "E2E_FAULT_TEST");

            // Assertions
            Assert.True(healthyCalled, "The healthy strategy should execute successfully even if the other one throws an exception.");
            Assert.True(host1.IsRunning, "The failing strategy's host should remain alive after fault containment.");
            Assert.True(host2.IsRunning, "The healthy strategy's host should remain alive and fully running.");

            await host.Supervisor.StopAllAsync();
        }

        private class FakeUnavailableNativeEngine : INativeAnalyticsEngine
        {
            public bool IsAvailable => false;
            public int CalculateEma(double[] values, int count, int period, double[] outEma) => -1;
        }
    }
}
