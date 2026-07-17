using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Port interface defining database operations for completed and active quantitative experiences.
    /// Acts as the primary boundary for the Auto-Train offline learning pipeline.
    /// </summary>
    public interface IExperienceRepository
    {
        /// <summary>
        /// Retrieves the most recent completed or active experience snapshots from the database.
        /// </summary>
        Task<IReadOnlyList<ExperienceRecord>> GetRecentExperiencesAsync(int limit, CancellationToken ct = default);

        /// <summary>
        /// Updates the latest incomplete experience record for a specific symbol with real physical results.
        /// This creates Labelled Data (Reward/Loss) allowing the neural model to learn autonomously.
        /// </summary>
        Task CompleteExperienceAsync(string symbol, double realizedPips, CancellationToken cancellationToken = default);
    }
}