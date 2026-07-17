using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus.Application.Mt5;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Security;
using Nexus.Infrastructure.Adapters.Mt5;
using Nexus.Infrastructure.Mt5Bridge;
using Nexus.Desktop.Services;
using Nexus.Desktop.ViewModels;
using Nexus.Core.Interfaces;
using Nexus.Application.Analytics;
using Nexus.Infrastructure.Native;
using Nexus.Application.Dashboard;
using Nexus.Application.Pipeline; // Added
using Nexus.Execution.Gateways; // Added
using Microsoft.EntityFrameworkCore;
namespace Nexus.Desktop
{
    public partial class App : System.Windows.Application
    {
        private static IHost? _host;

        public static IHost Host => _host ?? throw new InvalidOperationException("Host not initialized.");

        public App()
        {
            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    #region Configuration & Core Services
                    services.AddSingleton<IAppConfigurationService>(sp => new AppConfigurationService());
                    services.AddSingleton<ISecretStore>(sp => new WindowsSecretStore());
                    services.AddSingleton<IDiagnosticService, DiagnosticService>();
                    #endregion

                    #region Database Bootstrappers & DbContext
                    services.AddSingleton<IDatabaseBootstrapper, SqliteDatabaseBootstrapper>();
                    services.AddSingleton<IDatabaseBootstrapper, PostgreSqlDatabaseBootstrapper>();

                    // Registers EF Core DbContext required by repositories
                    #region Dynamic Database Context Setup (PostgreSQL / SQLite Hybrid)
                    // REASON: Automatically detects the database provider from configuration.
                    // Allows plug-and-play local SQLite usage without requiring a running PostgreSQL server on the host machine.
                    services.AddDbContext<NexusDbContext>((serviceProvider, options) =>
                    {
                        var config = serviceProvider.GetRequiredService<IAppConfigurationService>();
                        var settings = config.GetSettings(); // Reads from active DatabaseSettings

                        // FIXED: Using 'SelectedProvider' instead of non-existent 'Provider'
                        string provider = settings.SelectedProvider ?? "SQLite";
                        string connString = settings.ConnectionString;

                        if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(connString))
                            {
                                // Generates a local file db path inside the App executables directory
                                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nexus_trading.db");
                                connString = $"Data Source={dbPath}";
                            }
                            options.UseSqlite(connString);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(connString))
                            {
                                connString = "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";
                            }
                            options.UseNpgsql(connString);
                        }
                    });
                    #endregion
                    #endregion

                    #region MT5 Bridge Components & Adapters
                    services.AddSingleton<IMt5BridgeClient, TcpMt5BridgeClient>();
                    services.AddSingleton<IMt5BridgeService, Mt5BridgeService>();
                    services.AddSingleton<MarketDataPipeline>();

                    services.AddSingleton<SimulatedMt5ConnectionService>();
                    services.AddSingleton<SimulatedMt5AccountService>();
                    services.AddSingleton<SimulatedMt5TradeService>();
                    services.AddSingleton<SimulatedMt5TradingService>();
                    services.AddSingleton<RealMt5BridgeAdapter>();
                    services.AddSingleton<RealMt5BridgeConnectionService>();
                    services.AddSingleton<RealMt5TradingService>();

                    services.AddSingleton<IMt5ConnectionService, RoutingMt5ConnectionService>();
                    services.AddSingleton<IMt5AccountService, RoutingMt5AccountService>();
                    services.AddSingleton<IMt5TradeService, RoutingMt5TradeService>();
                    services.AddSingleton<IMt5TradingService, RoutingMt5TradingService>();

                    services.AddSingleton<ITradingPlatformConnector, SimulatedTradingPlatformConnector>();
                    services.AddSingleton<IConnectionHealthMonitor, SimulatedConnectionHealthMonitor>();
                    #endregion

                    #region MT5 Operator Facade & ViewModel
                    services.AddSingleton<IMt5OperatorService, Mt5OperatorService>();
                    services.AddSingleton<Mt5TradingViewModel>();
                    #endregion

                    #region Intelligence & AI Stack Registration
                    services.AddSingleton<INativeCoreService, NativeCoreService>();
                    services.AddSingleton<IScenarioSearchEngine, Nexus.Application.Intelligence.ScenarioSearchEngine>();
                    services.AddSingleton<IMultiTimeframeConsensusEngine, Nexus.Application.Intelligence.MultiTimeframeConsensusEngine>();
                    services.AddSingleton<IExperienceCollector, Nexus.Application.Intelligence.ExperienceCollector>();

                    services.AddSingleton<INativeAnalyticsEngine, NativeAnalyticsEngine>();
                    services.AddSingleton<INeuralModelService, Nexus.AI.NeuralModelService>();
                    services.AddSingleton<ICurrencyStrengthEngine, Nexus.Application.Intelligence.CurrencyStrengthEngine>();
                    services.AddSingleton<IAccumulatorService, Nexus.Application.Intelligence.AccumulatorService>();
                    services.AddSingleton<IDecisionEngine, Nexus.Application.Intelligence.DecisionEngine>();
                    services.AddSingleton<IScenarioEvaluationEngine, Nexus.Application.Intelligence.ScenarioEvaluationEngine>();
                    services.AddSingleton<IPatternMemory, Nexus.Application.Intelligence.PatternMemory>();
                    services.AddSingleton<Nexus.Application.Intelligence.NativeMarketIntelligenceService>();
                    services.AddSingleton<NexusIntelligenceViewModel>();
                    #endregion
                    #region Auto-Trade Orchestration & Pipeline dependencies
                    // Helper factories and managers needed for real-time risk evaluation and logging
                    services.AddSingleton<OrderIntentFactory>();
                    services.AddSingleton<PreTradeRiskEvaluator>();

                    // REASON: Mapped core IRiskManager Port to the concrete DefaultRiskManager implementation
                    services.AddSingleton<Nexus.Core.Interfaces.IRiskManager, Nexus.Application.Pipeline.DefaultRiskManager>();

                    services.AddSingleton<ExecutionAuditService>();
                    
                    // REASON: Register concrete gateways required by RiskControlledExecutionEngine's constructor
                    services.AddSingleton<Nexus.Execution.Gateways.SimulationExecutionGateway>();
                    services.AddSingleton<Nexus.Execution.Gateways.MT5ExecutionGateway>();

                    // REASON: Register scoped Risk Engine and dependency controllers to prevent captive DB Context
                    services.AddScoped<Nexus.Execution.RiskControlledExecutionEngine>();
                    services.AddScoped<Nexus.Execution.Risk.IRiskExecutionGuard, Nexus.Execution.Risk.RiskExecutionGuard>();
                    services.AddScoped<Nexus.Execution.Management.PositionManager>();
                    services.AddScoped<Nexus.Execution.Auditing.IExecutionAuditService, Nexus.Infrastructure.Persistence.Repositories.DbExecutionAuditService>();

                    // REASON: Mapped Application Port to the newly created ExecutionGatewayAdapter (Scoped to resolve scoped engine safely)
                    services.AddScoped<Nexus.Application.Ports.IExecutionGateway, Nexus.Execution.Gateways.ExecutionGatewayAdapter>();

                    // REASON: Mapped Bounded-Context Execution Gateway to the concrete MT5 Execution Gateway (Singleton)
                    services.AddSingleton<Nexus.Execution.Gateways.IExecutionGateway, Nexus.Execution.Gateways.MT5ExecutionGateway>();

                    // Main Singleton Event Listener (Does not hold any scoped dependencies in constructor anymore)
                    services.AddSingleton<Nexus.Application.Pipeline.ExecutionCoordinator>();
                    #endregion

                    #region Offline Learning & Training Stack (Auto-Train)
                    services.AddSingleton<Nexus.Training.ModelRegistry>();
                    services.AddSingleton<Nexus.Training.IModelStorage, Nexus.Training.FileModelStorage>();
                    services.AddSingleton<Nexus.Training.ValidationEngine>();
                    services.AddSingleton<Nexus.Training.TimeframeLearningManager>();
                    services.AddSingleton<Nexus.Training.TrainingPipeline>();

                    // REASON: Replaced simple DataStore with the AlphaGo-style Deep Learning Platform
                    // Constructs the physical directory trees and evaluates closed trades for the replay buffer
                    services.AddSingleton<Nexus.Training.TradingLearningPlatform>();
                    services.AddSingleton<Nexus.Training.ExperienceReplayEngine>();
                    #endregion

                    #region Repositories (Scoped)
                    services.AddScoped<IUnitOfWork, Nexus.Infrastructure.Persistence.Repositories.UnitOfWork>();

                    services.AddScoped<IUnitOfWork, Nexus.Infrastructure.Persistence.Repositories.UnitOfWork>();
                    services.AddScoped<IAccountRepository, Nexus.Infrastructure.Persistence.Repositories.AccountRepository>();
                    services.AddScoped<IOrderRepository, Nexus.Infrastructure.Persistence.Repositories.OrderRepository>();
                    services.AddScoped<IPositionRepository, Nexus.Infrastructure.Persistence.Repositories.PositionRepository>();
                    services.AddScoped<IExperienceRepository, Nexus.Infrastructure.Persistence.Repositories.ExperienceRepository>();
                    #endregion

                    #region Dashboard & UI Application Services
                    services.AddSingleton<IMt5BridgeOperatorService, Mt5BridgeOperatorService>();
                    services.AddSingleton<INativeAnalyticsEngine, NativeAnalyticsEngine>();
                    services.AddSingleton<IDecisionEventStream, DecisionEventStream>();
                    services.AddSingleton<IMarketDashboardService, MarketDashboardService>();
                    services.AddSingleton<IDecisionDashboardService, DecisionDashboardService>();
                    services.AddSingleton<IExecutionDashboardService, ExecutionDashboardService>();
                    services.AddSingleton<ITrainingDashboardService, TrainingDashboardService>();
                    services.AddSingleton<ISystemHealthMonitorService, SystemHealthMonitorService>();
                    #endregion

                    #region Workspace ViewModels
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.DashboardViewModel>();
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.Mt5BridgeViewModel>();
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.MarketWatchViewModel>();
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.ManualDeskViewModel>();
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.DiagnosticsViewModel>();
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.TestConsoleViewModel>();
                    services.AddSingleton<Nexus.Desktop.ViewModels.Workspaces.SettingsViewModel>();
                    #endregion



                    #region Observability & Web Server (Background Workers)
                    services.AddSingleton<Nexus.Application.Observability.DiagnosticRingBuffer>();
                    services.AddHostedService<Nexus.Infrastructure.Mt5Bridge.LocalHttpApiServer>();

                    // REASON: Start the high-performance background tick bulk-writer to database on startup
                    services.AddHostedService<Nexus.Infrastructure.Workers.MarketDataIngestionWorker>();
                    #endregion

                    #region ViewModels & Windows
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                    #endregion
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            #region Architectural Bootstrapping Sequence
            // REASON: DB Schema compilation must occur BEFORE starting the host.
            try
            {
                using (var scope = Host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
                    dbContext.Database.EnsureCreated();
                }
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"[DB BOOTSTRAP ERROR] Pre-flight database setup failed: {dbEx.Message}");
            }

            // Start the background services and Kestrel REST server safely now that the DB is ready
            await Host.StartAsync();

            // Explicitly resolve pipeline to register real-time tick routing subscription
            Host.Services.GetRequiredService<MarketDataPipeline>();

            // Explicitly resolve execution coordinator to bind AI auto-trade subscriptions on startup
            Host.Services.GetRequiredService<Nexus.Application.Pipeline.ExecutionCoordinator>();

            #region PRO AUTOMATION: MT5 Bridge Auto-Connection
            // REASON: Automatically connects the C# core to Kestrel port 8080 on startup.
            // This prevents the user from having to manually navigate to the 'MT5 Bridge' tab and click 'Connect'.
            // Once connected, 'IsBridgeConnected' becomes true, which triggers XAUUSD auto-subscription.
            var config = Host.Services.GetRequiredService<IAppConfigurationService>();
            var settings = config.GetSettings();

            bool isRealMode = string.Equals(settings.Mt5Mode, "Real", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(settings.Mt5Mode, "RealBridge", StringComparison.OrdinalIgnoreCase);

            if (isRealMode)
            {
                var bridgeService = Host.Services.GetRequiredService<IMt5BridgeService>();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Safe delay to ensure Kestrel web server is fully booted and listening
                        await Task.Delay(2000);

                        await bridgeService.ConnectAsync("127.0.0.1", 8080);
                        Console.WriteLine("[App] Auto-connected to MT5 Bridge successfully.");

                        // REASON: Subscribing to EURUSD as it is guaranteed to be active on the MT5 Chart.
                        // This bypasses broker-specific gold naming restrictions and kicks off the tick stream instantly.
                        await bridgeService.SubscribeSymbolAsync("EURUSD");
                        Console.WriteLine("[App] Programmatic subscription sent for EURUSD.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[App] Auto-connection to MT5 Bridge failed: {ex.Message}");
                    }
                });
            }
            #endregion

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
            #endregion
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}