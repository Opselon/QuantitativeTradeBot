using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.AI.Entities;

namespace Nexus.Core.AI.Interfaces
{
    public interface IDatasetRegistry
    {
        Task RegisterDatasetAsync(DatasetMetadata metadata, CancellationToken ct = default);
        Task<DatasetMetadata?> GetDatasetAsync(string datasetId, CancellationToken ct = default);
    }
}