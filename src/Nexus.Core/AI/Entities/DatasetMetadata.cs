namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Immutable record representing a specific snapshot of training data.
    /// Never mutated. Represents exact state used for a training run.
    /// </summary>
    public sealed record DatasetMetadata(
        string DatasetId,
        string DatasetVersion,
        string FeatureVersion,
        string LabelVersion,
        string GeneratorVersion,
        string GitCommit,
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<string> Symbols,
        IReadOnlyList<string> Timeframes,
        long NumberOfSamples,
        string Hash,
        DateTime CreationTimeUtc
    );
}