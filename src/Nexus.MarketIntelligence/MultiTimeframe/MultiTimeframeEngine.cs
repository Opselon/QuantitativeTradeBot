using Nexus.Core.Entities;
using Nexus.Core.Enums;

namespace Nexus.MarketIntelligence.MultiTimeframe
{
    /// <summary>
    /// Synchronizes and aligns quantitative indicators across multiple timeframes (M1, M5, M15, M30, H1, H4, D1).
    /// </summary>
    public sealed class MultiTimeframeEngine
    {
        private static readonly TimeframeType[] TargetTimeframes =
        {
            TimeframeType.M1,
            TimeframeType.M5,
            TimeframeType.M15,
            TimeframeType.M30,
            TimeframeType.H1,
            TimeframeType.H4,
            TimeframeType.D1
        };

        /// <summary>
        /// Synchronizes and analyzes the specified timeframes to construct a unified <see cref="MultiTimeframeState"/>.
        /// </summary>
        /// <param name="symbol">The asset symbol.</param>
        /// <param name="timeframeData">A dictionary containing historical candle lists for each timeframe.</param>
        /// <returns>A unified, deterministic <see cref="MultiTimeframeState"/>.</returns>
        public MultiTimeframeState Synchronize(string symbol, IReadOnlyDictionary<TimeframeType, IReadOnlyList<Candle>> timeframeData)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));

            if (timeframeData == null)
                throw new ArgumentNullException(nameof(timeframeData));

            var assessments = new Dictionary<TimeframeType, TimeframeAssessment>();
            var timestamp = DateTime.UtcNow;

            #region Timeframe Analysis Loop

            foreach (var tf in TargetTimeframes)
            {
                if (!timeframeData.TryGetValue(tf, out var candles) || candles == null || candles.Count == 0)
                {
                    // Fallback to neutral assessment if data is missing for a timeframe
                    assessments[tf] = new TimeframeAssessment(tf, 0.0, 0.0, 0.1, "No Data");
                    continue;
                }

                // Ensure latest candle timestamp is tracked
                var latestCandle = candles[candles.Count - 1];
                if (latestCandle.Timestamp > timestamp)
                {
                    timestamp = latestCandle.Timestamp;
                }

                // Perform deterministic timeframe feature calculations
                double trend = CalculateTrendDirection(candles);
                double momentum = CalculateMomentum(candles);
                double volatility = CalculateVolatility(candles);
                string structure = DetectStructure(candles);

                assessments[tf] = new TimeframeAssessment(tf, trend, momentum, volatility, structure);
            }

            #endregion

            #region Alignment Aggregation

            // Trend Alignment Analysis
            double avgTrend = assessments.Values.Average(a => a.TrendDirection);
            string trendAlignment = avgTrend switch
            {
                > 0.3 => "Bullish",
                < -0.3 => "Bearish",
                _ => "Mixed"
            };

            // Momentum Alignment Analysis
            double avgMomentum = assessments.Values.Average(a => a.Momentum);
            string momentumAlignment = avgMomentum switch
            {
                > 0.3 => "Bullish",
                < -0.3 => "Bearish",
                _ => "Mixed"
            };

            // Volatility Alignment Analysis
            double avgVolatility = assessments.Values.Average(a => a.Volatility);
            string volatilityAlignment = avgVolatility switch
            {
                > 0.4 => "Expanding",
                < 0.15 => "Compressing",
                _ => "Stable"
            };

            // Structure Alignment Analysis
            int breakoutCount = assessments.Values.Count(a => a.Structure.Contains("Breakout"));
            int rangeCount = assessments.Values.Count(a => a.Structure.Contains("Range"));
            string structureAlignment = "Neutral";
            if (breakoutCount >= 3)
            {
                structureAlignment = "Breakout";
            }
            else if (rangeCount >= 3)
            {
                structureAlignment = "Range Bound";
            }

            #endregion

            #region Consensus Score Calculation

            // Higher timeframes get higher weight for macro direction.
            // D1: 25%, H4: 20%, H1: 15%, M30: 15%, M15: 10%, M5: 10%, M1: 5%
            var weights = new Dictionary<TimeframeType, double>
            {
                { TimeframeType.D1, 0.25 },
                { TimeframeType.H4, 0.20 },
                { TimeframeType.H1, 0.15 },
                { TimeframeType.M30, 0.15 },
                { TimeframeType.M15, 0.10 },
                { TimeframeType.M5, 0.10 },
                { TimeframeType.M1, 0.05 }
            };

            double weightedScoreSum = 0.0;
            foreach (var tf in TargetTimeframes)
            {
                var assessment = assessments[tf];
                double weight = weights[tf];

                // Map TrendDirection (-1 to 1) and Momentum (-1 to 1) to a 0-100 scale for this timeframe
                double tfTrendScore = (assessment.TrendDirection + 1.0) * 50.0; // 0 to 100
                double tfMomentumScore = (assessment.Momentum + 1.0) * 50.0; // 0 to 100

                // Combine them equally for the timeframe's individual consensus
                double tfConsensus = (tfTrendScore * 0.6) + (tfMomentumScore * 0.4);

                weightedScoreSum += tfConsensus * weight;
            }

            #endregion

            return new MultiTimeframeState(
                symbol,
                timestamp,
                trendAlignment,
                momentumAlignment,
                volatilityAlignment,
                structureAlignment,
                weightedScoreSum,
                assessments);
        }

        #region Private Quantitative Helper Methods

        private static double CalculateTrendDirection(IReadOnlyList<Candle> candles)
        {
            if (candles.Count < 2) return 0.0;

            // Simple Moving Average comparison (using Close vs SMA of 5 or all if fewer)
            int period = Math.Min(candles.Count, 5);
            double sum = 0.0;
            for (int i = candles.Count - period; i < candles.Count; i++)
            {
                sum += candles[i].Close.Value;
            }
            double sma = sum / period;
            double latestClose = candles[candles.Count - 1].Close.Value;

            double diffPct = (latestClose - sma) / sma;
            // Map small percent differences to a range of [-1.0, 1.0]
            double scaledTrend = diffPct * 200.0; // E.g., 0.5% deviation becomes 1.0 direction
            return Math.Clamp(scaledTrend, -1.0, 1.0);
        }

        private static double CalculateMomentum(IReadOnlyList<Candle> candles)
        {
            if (candles.Count < 3) return 0.0;

            // Rate of Change (ROC) over 3 candles
            double startClose = candles[candles.Count - 3].Close.Value;
            double endClose = candles[candles.Count - 1].Close.Value;

            if (Math.Abs(startClose) < 1e-9) return 0.0;

            double roc = (endClose - startClose) / startClose;
            return Math.Clamp(roc * 300.0, -1.0, 1.0); // scaled to fit momentum representation
        }

        private static double CalculateVolatility(IReadOnlyList<Candle> candles)
        {
            if (candles.Count < 5) return 0.15; // default stable volatility

            // Standard deviation of close prices divided by mean close price
            double sum = 0.0;
            for (int i = 0; i < candles.Count; i++)
            {
                sum += candles[i].Close.Value;
            }
            double mean = sum / candles.Count;
            if (mean <= 0.0) return 0.15;

            double sumSquares = 0.0;
            for (int i = 0; i < candles.Count; i++)
            {
                double diff = candles[i].Close.Value - mean;
                sumSquares += diff * diff;
            }
            double variance = sumSquares / candles.Count;
            double stdDev = Math.Sqrt(variance);

            double relativeVolatility = stdDev / mean;
            return Math.Clamp(relativeVolatility * 100.0, 0.0, 1.0); // scaled relative volatility
        }

        private static string DetectStructure(IReadOnlyList<Candle> candles)
        {
            if (candles.Count < 5) return "Normal";

            var latest = candles[candles.Count - 1];
            double maxHighExcludingLatest = double.MinValue;
            double minLowExcludingLatest = double.MaxValue;

            for (int i = 0; i < candles.Count - 1; i++)
            {
                if (candles[i].High.Value > maxHighExcludingLatest)
                    maxHighExcludingLatest = candles[i].High.Value;
                if (candles[i].Low.Value < minLowExcludingLatest)
                    minLowExcludingLatest = candles[i].Low.Value;
            }

            if (latest.Close.Value > maxHighExcludingLatest)
                return "Bullish Breakout";
            if (latest.Close.Value < minLowExcludingLatest)
                return "Bearish Breakout";

            // If range between high and low is very narrow, classify as Range Bound
            double totalRange = maxHighExcludingLatest - minLowExcludingLatest;
            double averagePrice = (maxHighExcludingLatest + minLowExcludingLatest) / 2.0;
            if (averagePrice > 0.0 && (totalRange / averagePrice) < 0.002)
            {
                return "Tight Range Bound";
            }

            return "Normal Range";
        }

        #endregion
    }
}
