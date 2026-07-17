using System;

namespace Nexus.Infrastructure.Storage.Models
{
    // REASON: EF Core needs mutable classes with parameterless constructors for database mapping.
    // We keep these internal/hidden from the Domain and map them to the Immutable Records.

    public class DatasetMetadataDbModel
    {
        public string DatasetId { get; set; } = string.Empty;
        public string DatasetVersion { get; set; } = string.Empty;
        public string FeatureVersion { get; set; } = string.Empty;
        public string LabelVersion { get; set; } = string.Empty;
        public string GeneratorVersion { get; set; } = string.Empty;
        public string GitCommit { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SymbolsJson { get; set; } = "[]";
        public string TimeframesJson { get; set; } = "[]";
        public long NumberOfSamples { get; set; }
        public string Hash { get; set; } = string.Empty;
        public DateTime CreationTimeUtc { get; set; }
    }

    public class ModelMetadataDbModel
    {
        public string ModelId { get; set; } = string.Empty;
        public string ArchitectureType { get; set; } = string.Empty;
        public string Backend { get; set; } = string.Empty;
        public string DatasetId { get; set; } = string.Empty;
        public string ExperimentId { get; set; } = string.Empty;
        public string FeatureVersion { get; set; } = string.Empty;
        public string LabelVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckpointPath { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string GitCommit { get; set; } = string.Empty;
    }

    public class ExperimentDbModel
    {
        public string ExperimentId { get; set; } = string.Empty;
        public string ModelArchitecture { get; set; } = string.Empty;
        public string DatasetId { get; set; } = string.Empty;
        public int Epochs { get; set; }
        public double LearningRate { get; set; }
        public int BatchSize { get; set; }
        public string Optimizer { get; set; } = string.Empty;
        public string Seed { get; set; } = string.Empty;
        public string FinalMetricsJson { get; set; } = "{}";
        public string HyperparametersJson { get; set; } = "{}";
        public string HardwareInfo { get; set; } = string.Empty;
        public long TrainingDurationTicks { get; set; }
        public DateTime ExecutedAtUtc { get; set; }
    }
}