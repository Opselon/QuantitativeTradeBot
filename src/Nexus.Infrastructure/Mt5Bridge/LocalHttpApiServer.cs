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
    public class LocalHttpApiServer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private WebApplication? _app;

        public LocalHttpApiServer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(5005);
            });

            // Register parent singletons in the nested Kestrel container
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IMt5BridgeService>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<INativeCoreService>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<MarketDataPipeline>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IAppConfigurationService>());
            builder.Services.AddSingleton(_serviceProvider.GetRequiredService<DiagnosticRingBuffer>());

            var app = builder.Build();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                LocalHttpApiRoutes.Map(endpoints);
            });

            _app = app;
            await _app.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_app != null)
            {
                await _app.StopAsync(cancellationToken);
            }
        }
    }
}
