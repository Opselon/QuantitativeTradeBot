using Nexus.Application.Mt5Bridge.Contracts;

namespace Nexus.Application.Ports
{
    public interface IMt5BridgeClient
    {
        event Action<BridgeMessageEnvelope>? OnMessageReceived;
        Task ConnectAsync(CancellationToken ct);
        Task DisconnectAsync(CancellationToken ct);
        Task<BridgeMessageEnvelope> SendAsync(BridgeMessageEnvelope request, CancellationToken ct);
    }
}
