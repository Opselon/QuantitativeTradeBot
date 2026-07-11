using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RealMt5BridgeSession : IMt5Session
    {
        private readonly IMt5BridgeClient _bridgeClient;
        public string SessionId { get; } = "MT5_REAL_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        public GatewayConnectionStatus Status { get; private set; } = GatewayConnectionStatus.Disconnected;

        public event Action<GatewayConnectionStatus>? OnStatusChanged;

        public RealMt5BridgeSession(IMt5BridgeClient bridgeClient)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (Status == GatewayConnectionStatus.Connected) return;

            Status = GatewayConnectionStatus.Connecting;
            OnStatusChanged?.Invoke(Status);

            await _bridgeClient.ConnectAsync(cancellationToken);

            Status = GatewayConnectionStatus.Connected;
            OnStatusChanged?.Invoke(Status);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (Status == GatewayConnectionStatus.Disconnected) return;

            Status = GatewayConnectionStatus.Connecting;
            OnStatusChanged?.Invoke(Status);

            await _bridgeClient.DisconnectAsync(cancellationToken);

            Status = GatewayConnectionStatus.Disconnected;
            OnStatusChanged?.Invoke(Status);
        }

        public void Dispose()
        {
            Status = GatewayConnectionStatus.Disconnected;
        }
    }
}
