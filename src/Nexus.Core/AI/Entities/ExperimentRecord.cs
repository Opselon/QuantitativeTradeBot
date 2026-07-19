namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Tracks every training execution ensuring absolute reproducibility.
    /// </summary>
    public sealed record ExperimentRecord(
        string ExperimentId,
        string ModelArchitecture,
        string DatasetId,
        int Epochs,
        double LearningRate,
        int BatchSize,
        string Optimizer,
        string Seed,
        IReadOnlyDictionary<string, double> FinalMetrics,
        IReadOnlyDictionary<string, string> Hyperparameters,
        string HardwareInfo,
        TimeSpan TrainingDuration,
        DateTime ExecutedAtUtc
    );
}