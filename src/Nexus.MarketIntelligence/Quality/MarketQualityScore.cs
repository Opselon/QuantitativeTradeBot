namespace Nexus.MarketIntelligence.Quality
{
    /// <summary>
    /// Holds the normalized sub-scores and the final consolidated Market Quality Score.
    /// Represents overall market health and execution suitability on a 0-100 scale.
    /// </summary>
    public sealed class MarketQualityScore
    {
        /// <summary>
        /// Gets the final aggregated Market Quality Score on a scale of 0 to 100.
        /// Higher is better, indicating deep liquidity, tight spreads, low noise, and stable execution.
        /// </summary>
        public double OverallScore { get; }

        /// <summary>
        /// Gets the Liquidity rating on a scale of 0 to 100.
        /// </summary>
        public double Liquidity { get; }

        /// <summary>
        /// Gets the Spread quality rating on a scale of 0 to 100 (100 means extremely tight/cheap).
        /// </summary>
        public double Spread { get; }

        /// <summary>
        /// Gets the price smoothness/absence of noise rating on a scale of 0 to 100 (100 means perfectly smooth trend progress).
        /// </summary>
        public double NoiseQuality { get; }

        /// <summary>
        /// Gets the Trend Quality rating on a scale of 0 to 100.
        /// </summary>
        public double TrendQuality { get; }

        /// <summary>
        /// Gets the Volatility Stability rating on a scale of 0 to 100 (100 means stable, 0 means wildly fluctuating/erratic).
        /// </summary>
        public double VolatilityStability { get; }

        /// <summary>
        /// Gets the estimated Execution Risk rating on a scale of 0 to 100 (0 means low risk/easy fills, 100 means high risk/slippage expected).
        /// </summary>
        public double ExecutionRisk { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MarketQualityScore"/>.
        /// </summary>
        public MarketQualityScore(
            double overallScore,
            double liquidity,
            double spread,
            double noiseQuality,
            double trendQuality,
            double volatilityStability,
            double executionRisk)
        {
            OverallScore = Math.Clamp(overallScore, 0.0, 100.0);
            Liquidity = Math.Clamp(liquidity, 0.0, 100.0);
            Spread = Math.Clamp(spread, 0.0, 100.0);
            NoiseQuality = Math.Clamp(noiseQuality, 0.0, 100.0);
            TrendQuality = Math.Clamp(trendQuality, 0.0, 100.0);
            VolatilityStability = Math.Clamp(volatilityStability, 0.0, 100.0);
            ExecutionRisk = Math.Clamp(executionRisk, 0.0, 100.0);
        }

        /// <summary>
        /// Returns a string representation of the market quality metrics.
        /// </summary>
        public override string ToString() =>
            $"Market Quality: {OverallScore:F1}/100 | Liq:{Liquidity:F0} Spd:{Spread:F0} Noise:{NoiseQuality:F0} Trend:{TrendQuality:F0} Vol:{VolatilityStability:F0} Risk:{ExecutionRisk:F0}";
    }
}
