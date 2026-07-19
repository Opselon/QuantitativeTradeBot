// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   CORE LAYER (Interfaces / Ports)
// FILE:    IExperienceMemory.cs
// REFERENCED BY:
//   - src/Nexus.Training/ExperienceReplayEngine.cs
// ============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.AI.Interfaces
{
    /// <summary>
    /// Port interface defining the boundary for storing and querying episodic trading experiences.
    /// Supports prioritized experience replay and similarity-based pattern matching.
    /// </summary>
    public interface IExperienceMemory
    {
        /// <summary>
        /// Permanently inserts a completed trade's multidimensional experience context to memory.
        /// </summary>
        Task StoreExperienceAsync(DeepExperienceRecord record);

        /// <summary>
        /// Samples a randomized, prioritized batch of records for continuous model training.
        /// </summary>
        Task<IReadOnlyList<DeepExperienceRecord>> SampleBatchAsync(int batchSize);

        /// <summary>
        /// Queries experiences most statistically similar to the current market embedding vector.
        /// </summary>
        Task<IReadOnlyList<DeepExperienceRecord>> QueryBySimilarityAsync(float[] queryVector, int limit);

        /// <summary>
        /// Flushes memory-held buffers onto physical persistent storage.
        /// </summary>
        Task FlushToDiskAsync();

        /// <summary>
        /// Purges the active experience cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the current running number of cached experience episodes.
        /// </summary>
        int Count { get; }
    }
}