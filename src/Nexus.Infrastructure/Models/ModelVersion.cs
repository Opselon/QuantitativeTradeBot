namespace Nexus.Infrastructure.Models
{
    /// <summary>
    /// Represents a specific version of an AI neural quantitative model.
    /// Tracks training metrics, status, and metadata details.
    /// </summary>
    public class ModelVersion
    {
        /// <summary>
        /// Gets or sets the unique version identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the model version number (e.g. "v1.0", "v1.1").
        /// </summary>
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date of the version model.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets descriptive training details (e.g. epochs, dataset span).
        /// </summary>
        public string TrainingInformation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model out-of-sample validation score (e.g. Sharpe, Accuracy, EV).
        /// </summary>
        public double ValidationScore { get; set; }

        /// <summary>
        /// Gets or sets the active life cycle status of this version.
        /// </summary>
        public ModelStatus Status { get; set; } = ModelStatus.Draft;
    }
}
