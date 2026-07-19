using Nexus.Core.Entities;
using Nexus.MarketIntelligence.Features;
using Nexus.MarketIntelligence.MultiTimeframe;
using Nexus.MarketIntelligence.Quality;
using Nexus.MarketIntelligence.Regimes;

namespace Nexus.MarketIntelligence
{
    /// <summary>
    /// Represents the ultimate, explainable, high-quality unified market snapshot.
    /// Acts as the single source of truth consumed by downstream AI and decision engines.
    /// </summary>
    public sealed class MarketIntelligenceSnapshot
    {
        /// <summary>
        /// Gets the asset symbol.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Gets the UTC timestamp when this snapshot was synthesized.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Gets the normalized domain-compatible <see cref="MarketState"/> entity.
        /// </summary>
        public MarketState MarketState { get; }

        /// <summary>
        /// Gets the detailed multi-timeframe alignment analysis.
        /// </summary>
        public MultiTimeframeState MultiTimeframeState { get; }

        /// <summary>
        /// Gets the dominant classified market regime.
        /// </summary>
        public RegimeClassification DominantRegime { get; }

        /// <summary>
        /// Gets all evaluated regime classifications.
        /// </summary>
        public IReadOnlyDictionary<string, RegimeClassification> Regimes { get; }

        /// <summary>
        /// Gets the multidimensional market quality score and execution risk assessment.
        /// </summary>
        public MarketQualityScore QualityScore { get; }

        /// <summary>
        /// Gets the deterministic feature set extracted for downstream AI consumption.
        /// </summary>
        public ExtractedFeatures Features { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MarketIntelligenceSnapshot"/>.
        /// </summary>
        public MarketIntelligenceSnapshot(
            string symbol,
            DateTime timestampUtc,
            MarketState marketState,
            MultiTimeframeState multiTimeframeState,
            RegimeClassification dominantRegime,
            IReadOnlyDictionary<string, RegimeClassification> regimes,
            MarketQualityScore qualityScore,
            ExtractedFeatures features)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            TimestampUtc = timestampUtc;
            MarketState = marketState ?? throw new ArgumentNullException(nameof(marketState));
            MultiTimeframeState = multiTimeframeState ?? throw new ArgumentNullException(nameof(multiTimeframeState));
            DominantRegime = dominantRegime ?? throw new ArgumentNullException(nameof(dominantRegime));
            Regimes = regimes ?? throw new ArgumentNullException(nameof(regimes));
            QualityScore = qualityScore ?? throw new ArgumentNullException(nameof(qualityScore));
            Features = features ?? throw new ArgumentNullException(nameof(features));
        }
    }
}
