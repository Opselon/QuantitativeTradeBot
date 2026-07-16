using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Port interface defining database read operations for completed and active quantitative experiences.
    /// </summary>
    public interface IExperienceRepository
    {
        /// <summary>
        /// Retrieves the most recent completed or active experience snapshots from the database.
        /// </summary>
        Task<IReadOnlyList<ExperienceRecord>> GetRecentExperiencesAsync(int limit, CancellationToken ct = default);
    }
}
