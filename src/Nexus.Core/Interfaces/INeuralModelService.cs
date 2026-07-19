using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    public enum ModelMode
    {
        ONNX_MODEL,
        FALLBACK_MODE
    }

    /// <summary>
    /// Service contract for loading ONNX neural evaluation models and executing inference payloads.
    /// </summary>
    public interface INeuralModelService
    {
        string CurrentModelName { get; }
        string ModelVersion { get; }
        bool IsLoaded { get; }
        double InferenceLatencyMs { get; }
        DateTime LastExecutionTime { get; }
        ModelMode CurrentMode { get; }

        Task<bool> LoadModelAsync(string modelPath, CancellationToken cancellationToken = default);
        Task<EvaluationResult> EvaluateAsync(MarketVector vector, CancellationToken cancellationToken = default);
    }
}
