namespace Nexus.Infrastructure.Models
{
    /// <summary>
    /// Parent structural container describing general AI Model details and tracking its historical training versions.
    /// </summary>
    public class ModelMetadata
    {
        /// <summary>
        /// Gets or sets the name of the AI quantitative model.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the model purpose or targeted instruments.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the history of model versions generated or validated.
        /// </summary>
        public List<ModelVersion> Versions { get; set; } = new();
    }
}
