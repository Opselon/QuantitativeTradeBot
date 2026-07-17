using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.AI.Entities;

namespace Nexus.Core.AI.Interfaces
{
    /// <summary>
    /// Backend-agnostic contract for running model predictions.
    /// Implementations will use TorchSharp, ONNX, etc.
    /// </summary>
    public interface IInferenceEngine
    {
        Task LoadModelAsync(ModelMetadata metadata, CancellationToken ct = default);
        Task<Prediction> PredictAsync(double[] normalizedFeatures, CancellationToken ct = default);
        void UnloadModel();
    }
}