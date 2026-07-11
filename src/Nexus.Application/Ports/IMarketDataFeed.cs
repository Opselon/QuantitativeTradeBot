using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Ports
{
    public interface IMarketDataFeed
    {
        event Func<PriceTickEnvelope, Task>? OnTickReceived;
        Task SubscribeAsync(string symbol, CancellationToken cancellationToken = default);
        Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default);
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
