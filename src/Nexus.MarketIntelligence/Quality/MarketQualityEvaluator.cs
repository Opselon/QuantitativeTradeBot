using Nexus.Core.Entities;

namespace Nexus.MarketIntelligence.Quality
{
    /// <summary>
    /// Computes and evaluates multidimensional market quality scores and execution risks.
    /// </summary>
    public sealed class MarketQualityEvaluator
    {
        /// <summary>
        /// Calculates the market quality score based on price action history, spread, and liquidity attributes.
        /// </summary>
        /// <param name="candles">The chronologically ordered candle data.</param>
        /// <param name="currentSpreadPoints">Current spread in points.</param>
        /// <param name="averageSpreadPoints">Historical average spread in points.</param>
        /// <returns>A fully populated <see cref="MarketQualityScore"/>.</returns>
        public MarketQualityScore EvaluateQuality(
            IReadOnlyList<Candle> candles,
            double currentSpreadPoints,
            double averageSpreadPoints)
        {
            if (candles == null)
                throw new ArgumentNullException(nameof(candles));

            if (candles.Count < 5)
            {
                // Fallback for minimal data
                return new MarketQualityScore(50.0, 50.0, 50.0, 50.0, 50.0, 50.0, 30.0);
            }

            int count = candles.Count;
            var closes = candles.Select(c => c.Close.Value).ToList();
            var volumes = candles.Select(c => c.Volume.Value).ToList();
            var highLows = candles.Select(c => c.High.Value - c.Low.Value).ToList();

            #region 1. Liquidity Score

            double avgVolume = volumes.Average();
            // Map volume relative to a reference standard FX volume (e.g. 500 standard units per candle)
            double liquidityScore = Math.Clamp((avgVolume / 500.0) * 100.0, 20.0, 100.0);

            #endregion

            #region 2. Spread Quality Score

            // Lower spread is higher quality. If spread is 0, score is 100.
            // If spread rises, score decreases. Scale based on spread points (e.g., 2.0 points is normal for FX).
            double spreadRef = Math.Max(0.1, averageSpreadPoints);
            double currentSpreadRatio = currentSpreadPoints / spreadRef;
            double spreadScore = Math.Clamp(100.0 - (currentSpreadRatio * 20.0), 10.0, 100.0);

            #endregion

            #region 3. Noise Quality Score

            // Noise is high if price fluctuations (sum of high-low range) are much larger than the actual net price change.
            double totalRangeSum = highLows.Sum();
            double netChange = Math.Abs(closes[count - 1] - closes[0]);
            double noiseRatio = totalRangeSum > 0.0 ? netChange / totalRangeSum : 1.0;
            // NoiseQuality is 100 if noise is 0. If price fluctuations are high relative to progress, NoiseQuality is lower.
            double noiseQualityScore = Math.Clamp(noiseRatio * 150.0 + 30.0, 10.0, 100.0);

            #endregion

            #region 4. Trend Quality Score

            // Trend is quality if it is smooth and consistent without massive pullbacks.
            // Measure trend efficiency (net progress over sum of absolute changes)
            double absSumDiffs = 0.0;
            for (int i = 1; i < count; i++)
            {
                absSumDiffs += Math.Abs(closes[i] - closes[i - 1]);
            }
            double trendEfficiency = absSumDiffs > 0.0 ? netChange / absSumDiffs : 0.0;
            double trendQualityScore = Math.Clamp(trendEfficiency * 100.0, 10.0, 100.0);

            #endregion

            #region 5. Volatility Stability Score

            // Measures the standard deviation of high-low ranges. Wildly fluctuating ranges indicate unstable volatility.
            double avgHighLow = highLows.Average();
            double sumSqDiff = highLows.Sum(hl => Math.Pow(hl - avgHighLow, 2));
            double stdDevHL = Math.Sqrt(sumSqDiff / count);
            double relStdDevHL = avgHighLow > 0.0 ? stdDevHL / avgHighLow : 0.0;
            double volatilityStabilityScore = Math.Clamp(100.0 - (relStdDevHL * 100.0), 10.0, 100.0);

            #endregion

            #region 6. Execution Risk Score

            // Risk increases with wide spread, low liquidity, high noise, and volatile stability issues.
            double spreadPenalty = Math.Max(0.0, (currentSpreadPoints - averageSpreadPoints) * 15.0);
            double liquidityPenalty = Math.Max(0.0, 100.0 - liquidityScore) * 0.3;
            double volatilityPenalty = Math.Max(0.0, 100.0 - volatilityStabilityScore) * 0.3;

            double executionRiskScore = Math.Clamp(15.0 + spreadPenalty + liquidityPenalty + volatilityPenalty, 5.0, 95.0);

            #endregion

            #region Overall Score Aggregation

            // Weighted average of individual dimensions
            double weightedSum =
                (liquidityScore * 0.25) +
                (spreadScore * 0.25) +
                (noiseQualityScore * 0.15) +
                (trendQualityScore * 0.15) +
                (volatilityStabilityScore * 0.20);

            // Subtract scaled execution risk to arrive at overall market suitability
            double finalScore = Math.Clamp(weightedSum - (executionRiskScore * 0.1), 0.0, 100.0);

            #endregion

            return new MarketQualityScore(
                finalScore,
                liquidityScore,
                spreadScore,
                noiseQualityScore,
                trendQualityScore,
                volatilityStabilityScore,
                executionRiskScore);
        }
    }
}
