using Nexus.Core.Enums;

namespace Nexus.MarketIntelligence.Regimes
{
    /// <summary>
    /// Represents a detailed classification output for a single market regime candidate.
    /// </summary>
    public sealed class RegimeClassification
    {
        /// <summary>
        /// Gets the detected regime category name.
        /// (e.g., "Trending", "Range", "Breakout", "High Volatility", "Low Volatility", "Accumulation", "Distribution", "Manipulation Candidate", "Transition").
        /// </summary>
        public string Regime { get; }

        /// <summary>
        /// Gets the confidence of the classification, ranging from 0.0 to 100.0.
        /// </summary>
        public double Confidence { get; }

        /// <summary>
        /// Gets the estimated strength of the regime, ranging from 0.0 to 100.0.
        /// </summary>
        public double Strength { get; }

        /// <summary>
        /// Gets the explanation or mathematical justification for this regime classification.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RegimeClassification"/>.
        /// </summary>
        public RegimeClassification(string regime, double confidence, double strength, string reason)
        {
            Regime = regime ?? throw new ArgumentNullException(nameof(regime));
            Confidence = Math.Clamp(confidence, 0.0, 100.0);
            Strength = Math.Clamp(strength, 0.0, 100.0);
            Reason = reason ?? string.Empty;
        }

        /// <summary>
        /// Maps this classification output into the core domain's <see cref="MarketRegime"/> enum if possible.
        /// </summary>
        public MarketRegime ToDomainRegime()
        {
            return Regime switch
            {
                "Trending" => MarketRegime.TrendingBullish, // Simplification or defaults to bullish/bearish
                "Range" => MarketRegime.MeanReverting,
                "High Volatility" => MarketRegime.HighVolatility,
                "Low Volatility" => MarketRegime.LowVolatility,
                _ => MarketRegime.Unknown
            };
        }
    }
}
