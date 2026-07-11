using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Ports
{
    public interface IMt5AccountService
    {
        Task<AccountSnapshotDto> GetAccountSnapshotAsync(IMt5Session session, CancellationToken cancellationToken = default);
    }
}
