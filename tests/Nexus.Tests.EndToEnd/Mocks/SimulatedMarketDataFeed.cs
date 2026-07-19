using Nexus.Application.Ports;

namespace Nexus.Tests.EndToEnd.Mocks
{
    public class SimulatedMarketDataFeed : IMarketDataFeed
    {
        public event Func<PriceTickEnvelope, Task>? OnTickReceived;
        private long _sequence = 0;

        public Task SubscribeAsync(string symbol, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task PushTickAsync(string symbol, double bid, double ask)
        {
            var seq = Interlocked.Increment(ref _sequence);
            var env = new PriceTickEnvelope(symbol, DateTime.UtcNow, bid, ask, seq);
            if (OnTickReceived != null)
            {
                await OnTickReceived.Invoke(env);
            }
        }
    }
}
