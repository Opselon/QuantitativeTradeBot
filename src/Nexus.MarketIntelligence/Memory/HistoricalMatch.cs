using System;
using Nexus.Core.Entities;

namespace Nexus.MarketIntelligence.Memory
{
    /// <summary>
    /// Represents a matched historical market situation with similarity and outcome details.
    /// </summary>
    public sealed class HistoricalMatch
    {
        /// <summary>
        /// Gets the historical market state that matched.
        /// </summary>
        public MarketState HistoricalState { get; }

        /// <summary>
        /// Gets the similarity score between the query features and this matched historical state, ranging from 0.0 to 1.0.
        /// </summary>
        public double Similarity { get; }

        /// <summary>
        /// Gets the realized trading result/outcome associated with this past state (if any).
        /// </summary>
        public string Outcome { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HistoricalMatch"/>.
        /// </summary>
        public HistoricalMatch(MarketState historicalState, double similarity, string outcome)
        {
            HistoricalState = historicalState ?? throw new ArgumentNullException(nameof(historicalState));
            Similarity = Math.Clamp(similarity, 0.0, 1.0);
            Outcome = outcome ?? "Unknown";
        }
    }
}
