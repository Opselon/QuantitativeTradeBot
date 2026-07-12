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
                    // Register Services
                    services.AddSingleton<IAppConfigurationService>(sp => new AppConfigurationService());
                    services.AddSingleton<ISecretStore>(sp => new WindowsSecretStore());
                    services.AddSingleton<IDiagnosticService, DiagnosticService>();

                    // Register Bootstrappers
                    services.AddSingleton<IDatabaseBootstrapper, SqliteDatabaseBootstrapper>();
                    services.AddSingleton<IDatabaseBootstrapper, PostgreSqlDatabaseBootstrapper>();

                    // Register MT5 Bridge Components & Adapters
                    services.AddSingleton<IMt5BridgeClient, TcpMt5BridgeClient>();

                    // Register concrete underlying implementations
                    services.AddSingleton<SimulatedMt5ConnectionService>();
                    services.AddSingleton<SimulatedMt5AccountService>();
                    services.AddSingleton<SimulatedMt5TradeService>();
                    services.AddSingleton<SimulatedMt5TradingService>();
                    services.AddSingleton<RealMt5BridgeAdapter>();
                    services.AddSingleton<RealMt5BridgeConnectionService>();
                    services.AddSingleton<RealMt5TradingService>();

                    // Register Routing (dynamic selector) services
                    services.AddSingleton<IMt5ConnectionService, RoutingMt5ConnectionService>();
                    services.AddSingleton<IMt5AccountService, RoutingMt5AccountService>();
                    services.AddSingleton<IMt5TradeService, RoutingMt5TradeService>();
                    services.AddSingleton<IMt5TradingService, RoutingMt5TradingService>();

                    services.AddSingleton<ITradingPlatformConnector, SimulatedTradingPlatformConnector>();
                    services.AddSingleton<IConnectionHealthMonitor, SimulatedConnectionHealthMonitor>();

                    // Register MT5 Operator Facade & ViewModel
                    services.AddSingleton<IMt5OperatorService, Mt5OperatorService>();
                    services.AddSingleton<Mt5TradingViewModel>();

                    // Register Intelligence & AI Stack
                    services.AddSingleton<INativeAnalyticsEngine, NativeAnalyticsEngine>();
                    services.AddSingleton<INativeCoreService, NativeCoreService>();
                    services.AddSingleton<INeuralModelService, Nexus.AI.NeuralModelService>();
                    services.AddSingleton<ICurrencyStrengthEngine, Nexus.Application.Intelligence.CurrencyStrengthEngine>();
                    services.AddSingleton<IAccumulatorService, Nexus.Application.Intelligence.AccumulatorService>();
                    services.AddSingleton<IDecisionEngine, Nexus.Application.Intelligence.DecisionEngine>();
                    services.AddSingleton<IScenarioEvaluationEngine, Nexus.Application.Intelligence.ScenarioEvaluationEngine>();
                    services.AddSingleton<IPatternMemory, Nexus.Application.Intelligence.PatternMemory>();
                    services.AddSingleton<Nexus.Application.Intelligence.NativeMarketIntelligenceService>();
                    services.AddSingleton<NexusIntelligenceViewModel>();

                    // Register ViewModels & Windows
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host.StartAsync();

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
