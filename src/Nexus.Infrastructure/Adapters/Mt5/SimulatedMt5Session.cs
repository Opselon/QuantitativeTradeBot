using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedMt5Session : IMt5Session
    {
        public string SessionId { get; } = "MT5_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        public GatewayConnectionStatus Status { get; private set; } = GatewayConnectionStatus.Disconnected;

        public event Action<GatewayConnectionStatus>? OnStatusChanged;

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (Status == GatewayConnectionStatus.Connected) return;

            Status = GatewayConnectionStatus.Connecting;
            OnStatusChanged?.Invoke(Status);

            // Simulate connection delay
            await Task.Delay(1000, cancellationToken);

            Status = GatewayConnectionStatus.Connected;
            OnStatusChanged?.Invoke(Status);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (Status == GatewayConnectionStatus.Disconnected) return;

            Status = GatewayConnectionStatus.Connecting; // transitioning
            OnStatusChanged?.Invoke(Status);

            await Task.Delay(500, cancellationToken);

            Status = GatewayConnectionStatus.Disconnected;
            OnStatusChanged?.Invoke(Status);
        }

        public void Dispose()
        {
            Status = GatewayConnectionStatus.Disconnected;
        }
    }
}
