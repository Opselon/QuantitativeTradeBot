using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Ports
{
    public interface IExecutionGateway
    {
        Task<ExecutionReport> ExecuteAsync(ExecutionCommand command, CancellationToken cancellationToken = default);
    }
}
