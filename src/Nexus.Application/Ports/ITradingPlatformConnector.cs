using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Ports
{
    public interface ITradingPlatformConnector
    {
        string PlatformName { get; }
        Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default);
    }
}
