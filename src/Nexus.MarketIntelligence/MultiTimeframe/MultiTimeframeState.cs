using Nexus.Core.Enums;

namespace Nexus.MarketIntelligence.MultiTimeframe
{
    /// <summary>
    /// Represents the synchronized multi-timeframe state across M1, M5, M15, M30, H1, H4, and D1.
    /// Provides assessments for Trend, Momentum, Volatility, and Price Structure alignments.
    /// </summary>
    public sealed class MultiTimeframeState
    {
        /// <summary>
        /// Gets the symbol for this multi-timeframe state.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Gets the timestamp when this snapshot was evaluated.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Gets the consolidated trend alignment across all timeframes.
        /// (e.g., "Bullish", "Bearish", "Mixed", "Neutral").
        /// </summary>
        public string TrendAlignment { get; }

        /// <summary>
        /// Gets the consolidated momentum alignment across all timeframes.
        /// (e.g., "Bullish", "Bearish", "Mixed", "Neutral").
        /// </summary>
        public string MomentumAlignment { get; }

        /// <summary>
        /// Gets the consolidated volatility alignment across all timeframes.
        /// (e.g., "Expanding", "Compressing", "Stable", "Extremely Volatile").
        /// </summary>
        public string VolatilityAlignment { get; }

        /// <summary>
        /// Gets the consolidated structure alignment (e.g., "Breakout", "Range Bound", "Reverting", "Neutral").
        /// </summary>
        public string StructureAlignment { get; }

        /// <summary>
        /// Gets the consolidated consensus score between 0 and 100.
        /// 0 represents absolute bearish consensus across all timeframes.
        /// 100 represents absolute bullish consensus across all timeframes.
        /// 50 represents absolute neutrality or complete conflict.
        /// </summary>
        public double ConsensusScore { get; }

        /// <summary>
        /// Gets the individual assessments for each evaluated timeframe.
        /// </summary>
        public IReadOnlyDictionary<TimeframeType, TimeframeAssessment> TimeframeAssessments { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTimeframeState"/> class.
        /// </summary>
        public MultiTimeframeState(
            string symbol,
            DateTime timestampUtc,
            string trendAlignment,
            string momentumAlignment,
            string volatilityAlignment,
            string structureAlignment,
            double consensusScore,
            IReadOnlyDictionary<TimeframeType, TimeframeAssessment> timeframeAssessments)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            TimestampUtc = timestampUtc;
            TrendAlignment = trendAlignment ?? "Neutral";
            MomentumAlignment = momentumAlignment ?? "Neutral";
            VolatilityAlignment = volatilityAlignment ?? "Stable";
            StructureAlignment = structureAlignment ?? "Neutral";
            ConsensusScore = Math.Clamp(consensusScore, 0.0, 100.0);
            TimeframeAssessments = timeframeAssessments ?? new Dictionary<TimeframeType, TimeframeAssessment>();
        }
    }

    /// <summary>
    /// Holds the computed indicator scores and indicators for a single timeframe.
    /// </summary>
    public sealed class TimeframeAssessment
    {
        /// <summary>
        /// Gets the timeframe type.
        /// </summary>
        public TimeframeType Timeframe { get; }

        /// <summary>
        /// Gets the trend direction (-1.0 for Bearish, 0.0 for Neutral, 1.0 for Bullish).
        /// </summary>
        public double TrendDirection { get; }

        /// <summary>
        /// Gets the momentum score (-1.0 to 1.0).
        /// </summary>
        public double Momentum { get; }

        /// <summary>
        /// Gets the volatility value (normalized standard deviation or ATR ratio).
        /// </summary>
        public double Volatility { get; }

        /// <summary>
        /// Gets the structure classification (e.g., "High-Low Breakout", "Double Bottom", "Normal").
        /// </summary>
        public string Structure { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeframeAssessment"/> class.
        /// </summary>
        public TimeframeAssessment(TimeframeType timeframe, double trendDirection, double momentum, double volatility, string structure)
        {
            Timeframe = timeframe;
            TrendDirection = Math.Clamp(trendDirection, -1.0, 1.0);
            Momentum = Math.Clamp(momentum, -1.0, 1.0);
            Volatility = Math.Max(0.0, volatility);
            Structure = structure ?? "Normal";
        }
    }
}
