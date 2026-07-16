using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.MarketIntelligence.Features;

namespace Nexus.MarketIntelligence.Memory
{
    /// <summary>
    /// Port interface defining the contract for recording and searching historical market states.
    /// Provides decoupled pattern matching without direct dependencies on learning pipelines.
    /// </summary>
    public interface IMarketStateMemory
    {
        /// <summary>
        /// Records a current market state snapshot along with its deterministic feature set.
        /// </summary>
        Task StoreStateAsync(
            MarketState state,
            ExtractedFeatures features,
            string outcome = "Pending",
            CancellationToken ct = default);

        /// <summary>
        /// Searches past recorded states to find situations similar to the provided current feature vector.
        /// </summary>
        /// <param name="symbol">The asset symbol.</param>
        /// <param name="currentFeatures">The current feature vector snapshot.</param>
        /// <param name="similarityThreshold">Cosine similarity threshold (e.g. 0.95 for 95% similarity).</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A list of similar past market snapshots with their match similarity scores.</returns>
        Task<IReadOnlyList<HistoricalMatch>> FindSimilarStatesAsync(
            string symbol,
            ExtractedFeatures currentFeatures,
            double similarityThreshold,
            CancellationToken ct = default);
    }
}
