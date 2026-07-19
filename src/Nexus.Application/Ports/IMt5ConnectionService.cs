using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Ports
{
    public interface IMt5ConnectionService
    {
        Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default);
        Task<IMt5Session> CreateSessionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default);
    }
}
