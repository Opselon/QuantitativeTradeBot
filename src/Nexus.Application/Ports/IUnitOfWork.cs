using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Ports
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
