namespace Nexus.Training
{
    public enum TrainingModelStatus
    {
        Experimental,
        Testing,
        Approved,
        Active,
        Rejected
    }

    /// <summary>
    /// Represents detailed metadata and metrics tracking for a learning model version.
    /// </summary>
    public sealed class ModelVersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string TrainingDataVersion { get; set; } = string.Empty;
        public double ValidationScore { get; set; }
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
        public TrainingModelStatus Status { get; set; } = TrainingModelStatus.Experimental;

        public ModelVersionInfo()
        {
        }

        public ModelVersionInfo(string version, string trainingDataVersion)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            TrainingDataVersion = trainingDataVersion ?? throw new ArgumentNullException(nameof(trainingDataVersion));
            CreatedDate = DateTime.UtcNow;
            Status = TrainingModelStatus.Experimental;
        }
    }
}
