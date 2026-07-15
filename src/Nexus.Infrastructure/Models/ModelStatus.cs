namespace Nexus.Infrastructure.Models
{
    /// <summary>
    /// Represents the current life cycle state of an AI quantitative model.
    /// </summary>
    public enum ModelStatus
    {
        /// <summary>
        /// Initial blueprint state before training.
        /// </summary>
        Draft,

        /// <summary>
        /// Active neural training pipeline.
        /// </summary>
        Training,

        /// <summary>
        /// Validated against test metrics and out-of-sample data.
        /// </summary>
        Validated,

        /// <summary>
        /// Currently running live or in paper-simulation routing engines.
        /// </summary>
        Active,

        /// <summary>
        /// Deprecated and replaced by a more current model version.
        /// </summary>
        Deprecated
    }
}
