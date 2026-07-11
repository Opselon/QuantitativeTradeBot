using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;

namespace Nexus.Application.Ports
{
    public interface IMt5BridgeClient
    {
        Task ConnectAsync(CancellationToken ct);
        Task DisconnectAsync(CancellationToken ct);
        Task<BridgeMessageEnvelope> SendAsync(BridgeMessageEnvelope request, CancellationToken ct);
    }
}
