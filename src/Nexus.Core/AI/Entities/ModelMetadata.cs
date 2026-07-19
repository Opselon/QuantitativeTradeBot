using Nexus.Core.AI.Enums;

namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Immutable record tracking a fully trained neural network or rule-based model.
    /// </summary>
    public sealed record ModelMetadata(
        string ModelId,
        string ArchitectureType, // e.g., "MLP", "LSTM", "TFT"
        ExecutionBackend Backend,
        string DatasetId,
        string ExperimentId,
        string FeatureVersion,
        string LabelVersion,
        ModelStatus Status,
        string CheckpointPath,
        DateTime CreatedAtUtc,
        string GitCommit
    );
}