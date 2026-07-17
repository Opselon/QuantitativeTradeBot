using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.AI.Datasets;
using Nexus.Application.AI.Decision;
using Nexus.Application.AI.Evaluation;
using Nexus.Application.AI.Features;
using Nexus.Application.Analytics;
using Nexus.Application.Dashboard;
using Nexus.Application.Intelligence;
using Nexus.Application.Mt5;
using Nexus.Application.Observability;
using Nexus.Application.Pipeline;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Core.AI.Entities; // Resolved ModelMetadata, DatasetMetadata, ExperimentRecord entities
using Nexus.Core.AI.Enums;
using Nexus.Core.AI.Interfaces;
using Nexus.Core.Interfaces;
using Nexus.Desktop.Services;
using Nexus.Desktop.ViewModels;
using Nexus.Execution.Gateways;
using Nexus.Execution.Management;
using Nexus.Infrastructure.Adapters.Mt5;
using Nexus.Infrastructure.Mt5Bridge;
using Nexus.Infrastructure.Native;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Persistence.Repositories;
using Nexus.Infrastructure.Security;
using Nexus.Infrastructure.Storage;     // Resolved for AI SQLite Database Storage
using Nexus.Infrastructure.TorchSharp;   // Resolved for TorchSharp DL Engine
using Nexus.Training;
using System.IO;
using System.Windows;

namespace Nexus.Desktop
{
    /// <summary>
    /// The primary bootstrapper of the Nexus Quantitative Workstation.
    /// Configures the Host builder, registers dependencies across clean architecture layers,
    /// and orchestrates the database migrations and MT5 auto-connection.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static IHost? _host;

        public static IHost Host => _host ?? throw new InvalidOperationException("Host not initialized.");

        public App()
        {
            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                // REASON: Fluent API logging configuration placed BEFORE services configuration to maintain MSBuild syntax.
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new Nexus.Infrastructure.Logging.HtmlLoggerProvider());
                })
                .ConfigureServices((context, services) =>
                {
                    #region Configuration & Core Services
                    services.AddSingleton<IAppConfigurationService>(sp => new AppConfigurationService());
                    services.AddSingleton<ISecretStore>(sp => new WindowsSecretStore());
                    services.AddSingleton<IDiagnosticService, DiagnosticService>();
                    #endregion

                    #region Database Bootstrappers & DbContext Setup
                    services.AddSingleton<IDatabaseBootstrapper, SqliteDatabaseBootstrapper>();
                    services.AddSingleton<IDatabaseBootstrapper, PostgreSqlDatabaseBootstrapper>();

                    // REASON: Automatically detects the database provider from configuration.
                    // Allows plug-and-play local SQLite usage without requiring a running PostgreSQL server on the host machine.
                    services.AddDbContext<NexusDbContext>((serviceProvider, options) =>
                    {
                        var config = serviceProvider.GetRequiredService<IAppConfigurationService>();
                        var settings = config.GetSettings();

                        string provider = settings.SelectedProvider ?? "SQLite";
                        string connString = settings.ConnectionString;

                        if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(connString))
                            {
                                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nexus_trading.db");
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

                    #region AI Machine Learning Platform (Nexus.Application.AI)
                    services.AddSingleton<AtrFeatureExtractor>();
                    services.AddSingleton<MomentumFeatureExtractor>();

                    // Register FeatureOrchestrator by collecting all IFeatureExtractors
                    services.AddSingleton<IFeatureExtractor>(sp => sp.GetRequiredService<AtrFeatureExtractor>());
                    services.AddSingleton<IFeatureExtractor>(sp => sp.GetRequiredService<MomentumFeatureExtractor>());
                    services.AddSingleton<FeatureOrchestrator>();

                    services.AddSingleton<DatasetGenerator>();
                    services.AddSingleton<ChampionChallengerEvaluator>();
                    services.AddSingleton<DecisionFusionEngine>();
                    services.AddSingleton<AiTradingOrchestrator>();
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

                    #region Intelligence & Rule Engines
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
                    services.AddSingleton<NativeMarketIntelligenceService>();
                    services.AddSingleton<NexusIntelligenceViewModel>();
                    #endregion

                    #region Auto-Trade Orchestration & Pipeline dependencies
                    // Helper factories and managers needed for real-time risk evaluation and logging
                    services.AddSingleton<OrderIntentFactory>();
                    services.AddSingleton<PreTradeRiskEvaluator>();

                    // REASON: Mapped core IRiskManager Port to the concrete DefaultRiskManager implementation
                    services.AddSingleton<IRiskManager, DefaultRiskManager>();

                    services.AddSingleton<ExecutionAuditService>();

                    // REASON: Register concrete gateways required by RiskControlledExecutionEngine's constructor
                    services.AddSingleton<SimulationExecutionGateway>();
                    services.AddSingleton<MT5ExecutionGateway>();

                    // REASON: Register scoped Risk Engine and dependency controllers to prevent captive DB Context
                    services.AddScoped<Nexus.Execution.RiskControlledExecutionEngine>();
                    services.AddScoped<Nexus.Execution.Risk.IRiskExecutionGuard, Nexus.Execution.Risk.RiskExecutionGuard>();
                    services.AddScoped<PositionManager>();
                    services.AddScoped<Nexus.Execution.Auditing.IExecutionAuditService, DbExecutionAuditService>();

                    // REASON: Mapped Application Port to the newly created ExecutionGatewayAdapter (Scoped to resolve scoped engine safely)
                    services.AddScoped<Nexus.Application.Ports.IExecutionGateway, ExecutionGatewayAdapter>();

                    // REASON: Mapped Bounded-Context Execution Gateway to the concrete MT5 Execution Gateway (Singleton)
                    services.AddSingleton<Nexus.Execution.Gateways.IExecutionGateway, MT5ExecutionGateway>();

                    // Main Singleton Event Listener (Does not hold any scoped dependencies in constructor anymore)
                    services.AddSingleton<ExecutionCoordinator>();
                    #endregion
                    #region Offline Learning & Training Stack (Auto-Train)
                    services.AddSingleton<Nexus.Training.ModelRegistry>();
                    services.AddSingleton<Nexus.Training.IModelStorage, Nexus.Training.FileModelStorage>();
                    services.AddSingleton<Nexus.Training.ValidationEngine>();
                    services.AddSingleton<Nexus.Training.TimeframeLearningManager>();
                    services.AddSingleton<Nexus.Training.TrainingPipeline>();

                    // REASON: Registered the AlphaGo-style Trading Learning Platform (builds local directories structure)
                    // replacing the legacy flat DataStore.
                    services.AddSingleton<Nexus.Training.TradingLearningPlatform>();
                    services.AddSingleton<Nexus.Training.ExperienceReplayEngine>();

                    // REASON: Register the concrete TorchSharp Deep Learning backend (IInferenceEngine)
                    services.AddNexusTorchBackend();

                    // REASON: Register the completely isolated AI SQLite Database file (nexus_training.db)
                    services.AddNexusAiStorage("Data Source=nexus_training.db");
                    #endregion

                    #region Repositories (Scoped)
                    services.AddScoped<IUnitOfWork, UnitOfWork>();
                    services.AddScoped<IAccountRepository, AccountRepository>();
                    services.AddScoped<IOrderRepository, OrderRepository>();
                    services.AddScoped<IPositionRepository, PositionRepository>();
                    services.AddScoped<IExperienceRepository, ExperienceRepository>();
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
                    services.AddSingleton<DiagnosticRingBuffer>();
                    services.AddHostedService<LocalHttpApiServer>();

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
            #region Architectural Bootstrapping Sequence (0 to 100 Production-Ready)

            // =================================================================
            // STEP 1: PRE-FLIGHT ERROR INTERCEPTION (High-Priority Crash Guard)
            // =================================================================
            // REASON: Initialize the Global Unhandled Exception Interceptors before any service boots.
            // This captures background task failures and prevents silent application crashes.
            var rootLogger = Host.Services.GetRequiredService<ILogger<App>>();
            Nexus.Infrastructure.Logging.AppDomainExceptionHook.Register(rootLogger);

            // REASON: WPF UI Thread Dispatcher Crashes are handled locally in the Presentation/UI layer.
            // This prevents the WPF application from silently closing in live trading while keeping Infrastructure dependency-free.
            this.DispatcherUnhandledException += (s, args) =>
            {
                rootLogger.LogError(args.Exception, "[WPF UI THREAD CRASH] Intercepted dispatcher unhandled exception. Recovering state.");
                args.Handled = true;
            };

            // =================================================================
            // STEP 2: HIGH-PERFORMANCE DATABASE ASYNC MIGRATIONS (Pre-flight Data Setup)
            // =================================================================
            // REASON: SQLite does not support PostgreSQL migrations on disk.
            // We implement a hybrid bootstrapper: If SQLite is active, we use EnsureCreatedAsync
            // to compile schema directly from C# domain. If PostgreSQL is active, we run MigrateAsync.
            try
            {
                using (var scope = Host.Services.CreateScope())
                {
                    var configService = scope.ServiceProvider.GetRequiredService<IAppConfigurationService>();

                    // FIXED: Renamed local variable 'settings' to 'dbSettings' to prevent scope collision
                    var dbSettings = configService.GetSettings();
                    string provider = dbSettings.SelectedProvider ?? "SQLite";

                    // 2.1 Initialize and bootstrap the Operational Database (nexus_trading.db)
                    var dbContext = scope.ServiceProvider.GetRequiredService<NexusDbContext>();

                    if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[DB BOOTSTRAP] SQLite active. Compiling local schemas safely...");
                        await dbContext.Database.EnsureCreatedAsync();
                    }
                    else
                    {
                        Console.WriteLine("[DB BOOTSTRAP] PostgreSQL active. Running server migrations...");
                        await dbContext.Database.MigrateAsync();
                    }

                    // 2.2 Initialize and create AI Storage Database (nexus_training.db)
                    var trainingDbContext = scope.ServiceProvider.GetRequiredService<TrainingDbContext>();
                    await trainingDbContext.Database.EnsureCreatedAsync();

                    Console.WriteLine("[DB BOOTSTRAP] Both database schemas successfully aligned.");
                }
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"[CRITICAL DB BOOTSTRAP ERROR] Pre-flight migrations failed: {dbEx.Message}");
            }

            // =================================================================
            // STEP 3: ACTIVATE THE HOST & WEB SERVER (Port 8080 Active)
            // =================================================================
            // Start the background services and Kestrel REST server safely now that the DB is ready
            await Host.StartAsync();

            // =================================================================
            // STEP 4: RESOLVE ORCHESTRATION PIPELINES (AI Auto-Trade Listeners)
            // =================================================================
            // Explicitly resolve pipeline to register real-time tick routing subscription
            Host.Services.GetRequiredService<MarketDataPipeline>();

            // Explicitly resolve execution coordinator to bind AI auto-trade subscriptions on startup
            Host.Services.GetRequiredService<Nexus.Application.Pipeline.ExecutionCoordinator>();

            // =================================================================
            // STEP 5: AUTONOMOUS STARTUP AI TRAINING (MLOps Cold Bootstrapping)
            // =================================================================
            // REASON: If the AI database has 0 registered models, automatically execute the 
            // Training Pipeline in the background using the physical JSON files in the ReplayBuffer.
            // This generates the first real model artifact (.dat), promotes it to CHAMPION,
            // and loads it into the Inference Engine, immediately transitioning the UI out of "Managed Fallback" mode!
            _ = Task.Run(async () =>
            {
                try
                {
                    // Safe delay to ensure the UI Thread is completely initialized
                    await Task.Delay(5000);

                    var modelRegistry = Host.Services.GetRequiredService<IModelRegistry>();
                    var currentChampion = await modelRegistry.GetChampionAsync();

                    if (currentChampion == null)
                    {
                        Console.WriteLine("[App] No active Champion model found. Initiating autonomous training cycle...");
                        var pipeline = Host.Services.GetRequiredService<TrainingPipeline>();

                        var datasetMetadata = new DatasetMetadata(
                            DatasetId: "DS_STARTUP_M15",
                            DatasetVersion: "1.0",
                            FeatureVersion: "1.0",
                            LabelVersion: "1.0",
                            GeneratorVersion: "1.0",
                            GitCommit: "INITIAL_STARTUP",
                            StartDate: DateTime.UtcNow.AddDays(-10),
                            EndDate: DateTime.UtcNow,
                            Symbols: new[] { "XAUUSD" },
                            Timeframes: new[] { "M15" },
                            NumberOfSamples: 17,
                            Hash: "INIT",
                            CreationTimeUtc: DateTime.UtcNow
                        );

                        // Execute training cycle
                        var trainedModel = await pipeline.ExecuteTrainingCycleAsync(
                            TimeframeLearningCategory.Scalping,
                            "v1.0.0",
                            "DS_STARTUP_M15"
                        );

                        if (trainedModel != null)
                        {
                            Console.WriteLine($"[App] Autonomous training successful! Promoting Model {trainedModel.Version} to CHAMPION.");

                            // Re-map and register in ModelRegistry as Champion
                            var modelMetadata = new ModelMetadata(
                                ModelId: "v1.0.0",
                                ArchitectureType: "MLP",
                                Backend: ExecutionBackend.TorchSharp,
                                DatasetId: "DS_STARTUP_M15",
                                ExperimentId: "EXP_STARTUP",
                                FeatureVersion: "1.0",
                                LabelVersion: "1.0",
                                Status: ModelStatus.Champion,
                                CheckpointPath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusAI", "Checkpoints", "v1.0.0.dat"),
                                CreatedAtUtc: DateTime.UtcNow,
                                GitCommit: "INITIAL_STARTUP"
                            );

                            await modelRegistry.RegisterModelAsync(modelMetadata);
                            await modelRegistry.UpdateModelStatusAsync("v1.0.0", ModelStatus.Champion);

                            // Load into the active Inference Engine immediately
                            var inferenceEngine = Host.Services.GetRequiredService<IInferenceEngine>();
                            await inferenceEngine.LoadModelAsync(modelMetadata);

                            Console.WriteLine("[App] Live Hot-Swap complete! Champion Model v1.0.0 is now active.");
                        }
                    }
                    else
                    {
                        // Load existing Champion into the Inference Engine
                        var inferenceEngine = Host.Services.GetRequiredService<IInferenceEngine>();
                        await inferenceEngine.LoadModelAsync(currentChampion);
                        Console.WriteLine($"[App] Successfully loaded active Champion Model {currentChampion.ModelId} into Inference Engine.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Autonomous startup training failed: {ex.Message}");
                }
            });

            #endregion

            // =================================================================
            // STEP 6: MT5 BRIDGE AUTO-CONNECTION & LIVE TICK SIMULATOR
            // =================================================================
            #region PRO AUTOMATION: MT5 Bridge Auto-Connection & Live Tick Simulator
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

                        // Subscribing to EURUSD as it is guaranteed to be active on the MT5 Chart.
                        await bridgeService.SubscribeSymbolAsync("EURUSD");
                        Console.WriteLine("[App] Programmatic subscription sent for EURUSD.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[App] Auto-connection to MT5 Bridge failed: {ex.Message}");
                    }
                });
            }
            else
            {
                #region HIGH-FREQUENCY LIVE TICK SIMULATOR (Weekend & Offline Mock Feed)
                // REASON: If we are in Simulation/Paper mode, start an automated background task
                // that generates and POSTs a mock EURUSD price tick to our own Kestrel API every 2 seconds.
                // This simulates live trading conditions during weekends or offline dry-runs,
                // instantly awakening the AI Neural Decision Matrix and Market Intelligence Matrix on the UI.
                _ = Task.Run(async () =>
                {
                    var httpClient = new System.Net.Http.HttpClient();
                    var random = new Random();
                    double basePrice = 1.0850; // Base EURUSD price

                    // Wait 4 seconds for Kestrel and MainWindow to fully load
                    await Task.Delay(4000);

                    while (true)
                    {
                        try
                        {
                            // Send a mock tick every 2 seconds
                            await Task.Delay(2000);

                            double change = (random.NextDouble() - 0.5) * 0.0003; // Random micro fluctuation
                            basePrice += change;
                            double ask = basePrice + 0.00015;
                            double bid = basePrice;

                            string timestampStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                            // Compile the exact JSON envelope required by the tick ingestion API
                            string tickPayload = "{" +
                                "\"messageType\":\"Request\"," +
                                "\"requestId\":\"mock-tick-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "\"," +
                                "\"command\":\"ReceiveTickStream\"," +
                                "\"payload\":{" +
                                    "\"symbol\":\"EURUSD\"," +
                                    "\"timestamp\":\"" + timestampStr + "\"," +
                                    "\"bid\":" + bid.ToString("F5") + "," +
                                    "\"ask\":" + ask.ToString("F5") + "," +
                                    "\"spread\":0.00015," +
                                    "\"volume\":100.0" +
                                "}," +
                                "\"error\":null," +
                                "\"version\":\"1.0\"" +
                            "}";

                            var content = new System.Net.Http.StringContent(tickPayload, System.Text.Encoding.UTF8, "application/json");
                            await httpClient.PostAsync("http://localhost:8080/api/v1/bridge/tick", content);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Tick Simulator Error] Failed to inject mock tick: {ex.Message}");
                        }
                    }
                });
                #endregion
            }
            #endregion

            // =================================================================
            // STEP 7: SHOW MAIN WORKSTATION WINDOW (UI Launch)
            // =================================================================
            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
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