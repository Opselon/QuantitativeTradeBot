using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.AI.Entities;

namespace Nexus.Core.AI.Interfaces
{
    /// <summary>
    /// Standardized pipeline for training any backend model (MLP, LSTM, etc).
    /// </summary>
    public interface ITrainingPipeline
    {
        Task<ModelMetadata> ExecuteTrainingAsync(
            string experimentId,
            DatasetMetadata dataset,
            IExperimentTracker tracker,
            CancellationToken ct = default);
    }
}