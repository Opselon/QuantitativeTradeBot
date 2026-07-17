using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.AI.Entities;

namespace Nexus.Core.AI.Interfaces
{
    public interface IExperimentTracker
    {
        Task LogExperimentAsync(ExperimentRecord experiment, CancellationToken ct = default);
        Task<ExperimentRecord?> GetExperimentAsync(string experimentId, CancellationToken ct = default);
    }
}