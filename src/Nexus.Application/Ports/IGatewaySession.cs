namespace Nexus.Application.Ports
{
    public interface IGatewaySession
    {
        string SessionId { get; }
        GatewayConnectionStatus Status { get; }
        event Action<GatewayConnectionStatus>? OnStatusChanged;
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }
}
