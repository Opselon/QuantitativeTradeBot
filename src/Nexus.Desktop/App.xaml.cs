using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Security;
using Nexus.Infrastructure.Adapters.Mt5;
using Nexus.Desktop.Services;
using Nexus.Desktop.ViewModels;

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

                    // Register MT5 Adapters
                    services.AddSingleton<IMt5ConnectionService, SimulatedMt5ConnectionService>();
                    services.AddSingleton<IMt5AccountService, SimulatedMt5AccountService>();
                    services.AddSingleton<ITradingPlatformConnector, SimulatedTradingPlatformConnector>();
                    services.AddSingleton<IConnectionHealthMonitor, SimulatedConnectionHealthMonitor>();

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
