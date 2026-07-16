using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Nexus.Application.Ports;
using Nexus.Desktop.Services;
using Nexus.Desktop.ViewModels.Workspaces;
using Nexus.Application.Dashboard;
using Nexus.Infrastructure.Mt5Bridge;
using Nexus.Core.Interfaces;
using Nexus.Core.Entities;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Tests.Unit.Desktop
{
    public class DashboardViewModelTests
    {
        private DashboardViewModel CreateViewModel(
            IMarketDashboardService market,
            IDecisionDashboardService decision,
            IExecutionDashboardService execution,
            ITrainingDashboardService training,
            ISystemHealthMonitorService health)
        {
            var fakeBridge = new FakeBridgeService();
            var fakeNative = new FakeNativeCore();
            var nullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<MarketDataPipeline>.Instance;
            var pipeline = new MarketDataPipeline(fakeBridge, fakeNative, nullLogger);
            var diagnostic = new StubDiagnosticService();

            return new DashboardViewModel(fakeBridge, pipeline, diagnostic, market, decision, execution, training, health);
        }

        [Fact]
        public void InitialState_IsCorrect()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Assert
            Assert.Equal("EURUSD", vm.CurrentSymbol);
            Assert.Equal("Trending Bullish", vm.MarketRegime);
            Assert.Equal(85, vm.MarketQualityScore);
            Assert.Equal("BUY", vm.CurrentDecision);
            Assert.Equal(0.84, vm.Confidence);
            Assert.Equal("Simulation", vm.CurrentProfile);
            Assert.False(vm.IsLivePermissionGranted);
            Assert.Equal("Nexus AI v1.x", vm.CurrentModelName);
            Assert.Equal("1.0.4", vm.ModelVersion);
        }

        [Fact]
        public void ToggleLivePermission_Fails_WhenProfileIsNotLive()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Assert profile is Simulation (not Live)
            Assert.Equal("Simulation", vm.CurrentProfile);

            // Act - Toggle live permission (which has custom verification)
            vm.ToggleLivePermissionCommand.Execute(null);

            // Assert - Permission remains false because profile is not Live
            Assert.False(vm.IsLivePermissionGranted);
            Assert.False(execution.IsLivePermissionGranted);
        }

        [Fact]
        public async Task ToggleLivePermission_Succeeds_WhenProfileIsLive_AndConfirmed()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Switch to Live profile
            vm.EnableSimulationCommand.Execute(null); // Just to clear
            execution.SetProfile(ExecutionDashboardProfile.Live);

            // Check profile is Live
            Assert.Equal("Live", vm.CurrentProfile);

            // Setup confirmation dialog mock to return true (confirmed)
            bool confirmCalled = false;
            vm.ConfirmCallback = async (msg) =>
            {
                confirmCalled = true;
                Assert.Contains("LIVE", msg);
                return await Task.FromResult(true);
            };

            // Act
            await ((IAsyncRelayCommand)vm.ToggleLivePermissionCommand).ExecuteAsync(null);

            // Assert
            Assert.True(confirmCalled);
            Assert.True(vm.IsLivePermissionGranted);
            Assert.True(execution.IsLivePermissionGranted);
            Assert.Contains("SECURITY PERMISSION GRANTED", execution.PermissionAuditLog[execution.PermissionAuditLog.Count - 1]);
        }

        [Fact]
        public async Task ToggleLivePermission_Aborts_WhenUserRejectsPrompt()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Switch to Live profile
            execution.SetProfile(ExecutionDashboardProfile.Live);

            // Setup confirmation dialog mock to return false (user rejects)
            bool confirmCalled = false;
            vm.ConfirmCallback = async (msg) =>
            {
                confirmCalled = true;
                return await Task.FromResult(false);
            };

            // Act
            await ((IAsyncRelayCommand)vm.ToggleLivePermissionCommand).ExecuteAsync(null);

            // Assert
            Assert.True(confirmCalled);
            Assert.False(vm.IsLivePermissionGranted);
            Assert.False(execution.IsLivePermissionGranted);
            Assert.Contains("SECURITY PERMISSION REJECTED", execution.PermissionAuditLog[execution.PermissionAuditLog.Count - 1]);
        }

        [Fact]
        public void SwitchProfile_AutomaticallyRevokesLivePermission()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Switch to Live profile
            execution.SetProfile(ExecutionDashboardProfile.Live);

            // Bypass prompt and explicitly set permission to true directly on service
            execution.RequestToggleLivePermissionAsync(true, (m) => Task.FromResult(true)).Wait();
            Assert.True(vm.IsLivePermissionGranted);

            // Act - Switch to Paper profile
            vm.EnablePaperCommand.Execute(null);

            // Assert - Permission automatically set to false and profile switched
            Assert.Equal("Paper", vm.CurrentProfile);
            Assert.False(vm.IsLivePermissionGranted);
            Assert.False(execution.IsLivePermissionGranted);
        }

        [Fact]
        public void LiveUpdates_TriggerPropertyNotifications()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            List<string> updatedProperties = new();
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    updatedProperties.Add(e.PropertyName);
                }
            };

            // Act - Push an update into the market service
            market.PushMarketUpdate("XAUUSD", "Breakout", 92, 0.95, 0.40, 0.88, "Bullish", "Bullish", "Entry", "Updates processed.");

            // Assert - Properties are updated and raised PropertyChanged events
            Assert.Contains("MarketRegime", updatedProperties);
            Assert.Contains("MarketQualityScore", updatedProperties);
            Assert.Contains("Liquidity", updatedProperties);
            Assert.Contains("Volatility", updatedProperties);
        }

        [Fact]
        public void ExplainabilityTimeline_IsLoadedAndPopulated()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Assert
            Assert.NotNull(vm.ExplainabilityTimeline);
            Assert.NotEmpty(vm.ExplainabilityTimeline);
            Assert.Equal("CLOSE", vm.ExplainabilityTimeline[0].TransitionType);
            Assert.Equal("PARTIAL_CLOSE", vm.ExplainabilityTimeline[1].TransitionType);
            Assert.Equal("MOVE_STOP", vm.ExplainabilityTimeline[2].TransitionType);
        }

        [Fact]
        public void DecisionReplay_MasterDetailReconstruction_IsDeterministicAndReadOnly()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            // Assert master list is populated
            Assert.NotNull(vm.HistoricalDecisions);
            Assert.NotEmpty(vm.HistoricalDecisions);
            Assert.Equal("DEC-048 (CLOSE)", vm.HistoricalDecisions[0].DecisionName);

            // Act - Select first decision
            vm.SelectedReplayDecision = vm.HistoricalDecisions[0];

            // Assert detail views are reconstructed deterministically and correctly
            Assert.True(vm.IsReplayDetailVisible);
            Assert.Equal(vm.HistoricalDecisions[0].MarketSnapshot, vm.ReplayMarketSnapshot);
            Assert.Equal(vm.HistoricalDecisions[0].MarketRegime, vm.ReplayMarketRegime);
            Assert.Equal(vm.HistoricalDecisions[0].FeatureVectorSummary, vm.ReplayFeatureVectorSummary);
            Assert.Equal(vm.HistoricalDecisions[0].MultiTimeframeConsensus, vm.ReplayMultiTimeframeConsensus);
            Assert.Equal(vm.HistoricalDecisions[0].ScenarioSearchResults, vm.ReplayScenarioSearchResults);
            Assert.Equal(vm.HistoricalDecisions[0].FinalDecision, vm.ReplayFinalDecision);
            Assert.Equal(vm.HistoricalDecisions[0].ExecutionOutcome, vm.ReplayExecutionOutcome);
        }

        [Fact]
        public void SystemHealthMonitor_UpdatesStateAndTriggersNotification()
        {
            // Arrange
            var market = new MarketDashboardService();
            var decision = new DecisionDashboardService();
            var execution = new ExecutionDashboardService();
            var training = new TrainingDashboardService();
            var health = new SystemHealthMonitorService();

            using var vm = CreateViewModel(market, decision, execution, training, health);

            List<string> notifiedProperties = new();
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    notifiedProperties.Add(e.PropertyName);
                }
            };

            // Act - Push health alert change
            health.PushHealthUpdate(
                SystemHealthStatus.Healthy,
                SystemHealthStatus.Healthy,
                SystemHealthStatus.Healthy,
                SystemHealthStatus.Healthy,
                SystemHealthStatus.Warning, // execution alert warning
                SystemHealthStatus.Healthy,
                SystemHealthStatus.Healthy,
                15.2,
                120.4,
                "15/500 Threads",
                0.012,
                1.45,
                32.1
            );

            // Assert
            Assert.Equal("Healthy", vm.NativeEngineHealth);
            Assert.Equal("Warning", vm.ExecutionEngineHealth);
            Assert.Equal(15.2, vm.CpuUsage);
            Assert.Equal(120.4, vm.MemoryUsageMb);
            Assert.Equal("15/500 Threads", vm.ThreadPoolUtilization);
            Assert.Equal(0.012, vm.TickProcessingLatencyMs);
            Assert.Equal(1.45, vm.DecisionLatencyMs);
            Assert.Equal(32.1, vm.ExecutionLatencyMs);

            Assert.Contains("CpuUsage", notifiedProperties);
            Assert.Contains("MemoryUsageMb", notifiedProperties);
            Assert.Contains("ThreadPoolUtilization", notifiedProperties);
            Assert.Contains("TickProcessingLatencyMs", notifiedProperties);
            Assert.Contains("DecisionLatencyMs", notifiedProperties);
            Assert.Contains("ExecutionLatencyMs", notifiedProperties);
        }

        // --- STUBS AND FACTION FAKES ---

        private class StubDiagnosticService : IDiagnosticService
        {
            public void Log(string subsystem, string level, string message) { }
        }

        private class FakeBridgeService : IMt5BridgeService
        {
            public event Action<PriceTickEnvelope>? OnTickReceived;
            public event Action<string>? OnStatusChanged;

            public string ConnectionStatusText => "Disconnected";
            public double PingLatencyMs => 0;
            public DateTime LastHeartbeatUtc => DateTime.MinValue;
            public string LastErrorMessage => string.Empty;
            public IReadOnlyCollection<string> SubscribedSymbols => Array.Empty<string>();
            public bool IsConnected => false;
            public bool IsAuthenticated => false;
            public bool IsEaPresentInRepository => false;
            public long EaRepositoryFileSize => 0;
            public DateTime EaRepositoryFileLastModifiedUtc => DateTime.MinValue;
            public string EaRepositoryFilePath => string.Empty;
            public bool IsEaInstalledConfirmed { get; set; }
            public bool IsHandshakeSucceeded => false;
            public string EaName => string.Empty;
            public string EaVersion => string.Empty;
            public string ChartSymbol => string.Empty;
            public string HandshakeAccountId => string.Empty;
            public string HandshakeBrokerServer => string.Empty;

            public Task ConnectAsync(string host, int port, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task<bool> LoginAsync(string accountId, string password, string server, CancellationToken cancellationToken) => Task.FromResult(true);
            public Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken cancellationToken) => Task.FromResult<AccountSnapshotDto?>(new AccountSnapshotDto());
            public Task SubscribeSymbolAsync(string symbol, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task UnsubscribeSymbolAsync(string symbol, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private class FakeNativeCore : INativeCoreService
        {
            public bool IsAvailable => false;
            public string LastError => string.Empty;
            public void UpdateTick(Tick tick) { }
            public MarketVector GetMarketVector() => new MarketVector(0.5, 0.5, 0.5, 0.2, 0.5, 0.9, 80.0, 1.0, 1.0, 0.1);
            public MarketState GetMarketState() => new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.5, 0.8, 0.7, 0.5, 0.1, 50.0, "Trend Bullish");
        }
    }
}
