using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Contract for recording decisions and state metrics to feed continuous learning algorithms.
    /// </summary>
    public interface IExperienceRecorder
    {
        /// <summary>
        /// Persists an analytical market scenario-decision experience sample to historical databases.
        /// </summary>
        /// <param name="sample">The experience snapshot to record.</param>
        Task RecordExperienceAsync(ExperienceSample sample);
    }
}
