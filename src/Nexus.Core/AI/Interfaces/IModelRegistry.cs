using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Enums;

namespace Nexus.Core.AI.Interfaces
{
    public interface IModelRegistry
    {
        Task RegisterModelAsync(ModelMetadata model, CancellationToken ct = default);
        Task UpdateModelStatusAsync(string modelId, ModelStatus status, CancellationToken ct = default);
        Task<ModelMetadata?> GetChampionAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ModelMetadata>> GetCandidatesAsync(CancellationToken ct = default);
    }
}