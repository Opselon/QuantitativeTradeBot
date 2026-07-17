using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus.Application.Observability;
using Nexus.Application.Ports;
using Nexus.Core.Interfaces;

namespace Nexus.Infrastructure.Mt5Bridge
{
    /// <summary>
    /// Hosted background service running the Kestrel REST Server on Port 8080.
    /// Handles incoming HTTP traffic from MQL5 WebRequests.
    /// </summary>
    public class LocalHttpApiServer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private WebApplication? _app;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalHttpApiServer"/> class.
        /// </summary>
        public LocalHttpApiServer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #region NEW VERSION - Kestrel Server on Port 8080
        /// <summary>
        /// Configures and boots the WebApplication listening on localhost:8080.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // If an older app instance is running, stop and dispose it first.
            if (_app != null)
            {
                await _app.StopAsync(cancellationToken);
                await _app.DisposeAsync();
                _app = null;
            }

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.ConfigureKestrel(options =>
            {
                // Converted port 5005 -> 8080 to support native MT5 WebRequests directly
                options.ListenLocalhost(8080);
            });

            // Register parent singletons in Kestrel dependency injection
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IMt5BridgeService>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<INativeCoreService>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<MarketDataPipeline>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IAppConfigurationService>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<DiagnosticRingBuffer>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IMt5BridgeClient>());

            var app = builder.Build();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                LocalHttpApiRoutes.Map(endpoints);
            });

            _app = app;
            await _app.StartAsync(cancellationToken);

            Console.WriteLine("[LocalHttpApiServer] Kestrel REST Server listening on http://localhost:8080");
        }


        /// <summary>
        /// Shuts down the local WebApplication.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_app != null)
            {
                await _app.StopAsync(cancellationToken);
            }
        }
        #endregion
    }
}