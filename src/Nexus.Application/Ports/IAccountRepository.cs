using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Application.Ports
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default);
        Task UpsertAsync(Account account, CancellationToken cancellationToken = default);
    }
}
