using Nexus.Core.Entities;

namespace Nexus.MarketIntelligence.Regimes
{
    /// <summary>
    /// Analyzes candle sequences, volumes, spreads, and price deviations to detect active market regimes.
    /// Supports detecting: Trending, Range, Breakout, High Volatility, Low Volatility, Accumulation, Distribution, Manipulation Candidate, and Transition.
    /// </summary>
    public sealed class MarketRegimeDetector
    {
        /// <summary>
        /// Detects all applicable market regimes for a given candle series.
        /// </summary>
        /// <param name="candles">Historical candle data, ordered chronologically (oldest to newest).</param>
        /// <param name="currentSpreadPoints">The current trading spread in points/pips.</param>
        /// <returns>A dictionary of detected regimes mapped to their classifications.</returns>
        public IReadOnlyDictionary<string, RegimeClassification> DetectRegimes(IReadOnlyList<Candle> candles, double currentSpreadPoints)
        {
            if (candles == null)
                throw new ArgumentNullException(nameof(candles));

            var results = new Dictionary<string, RegimeClassification>();

            if (candles.Count < 5)
            {
                // Fallback for insufficient data
                var defaultUnknown = new RegimeClassification("Unknown", 100.0, 50.0, "Insufficient candle data for robust classification.");
                results["Unknown"] = defaultUnknown;
                return results;
            }

            #region Statistical Pre-computations

            int count = candles.Count;
            var closes = candles.Select(c => c.Close.Value).ToList();
            var volumes = candles.Select(c => c.Volume.Value).ToList();
            var highLows = candles.Select(c => c.High.Value - c.Low.Value).ToList();

            double avgClose = closes.Average();
            double avgVolume = volumes.Average();
            double avgHighLow = highLows.Average();

            // Calculate standard deviation of close prices (volatility proxy)
            double sumSqDiff = closes.Sum(c => Math.Pow(c - avgClose, 2));
            double stdDev = Math.Sqrt(sumSqDiff / count);
            double relStdDev = avgClose > 0.0 ? stdDev / avgClose : 0.0;

            // Trend strength (ratio of start-to-end price to sum of high-low ranges)
            double startClose = closes[0];
            double endClose = closes[count - 1];
            double priceProgress = endClose - startClose;
            double sumRanges = highLows.Sum();
            double trendEfficiency = sumRanges > 0.0 ? Math.Abs(priceProgress) / sumRanges : 0.0;

            // Volume trend
            double firstHalfVol = volumes.Take(count / 2).Average();
            double secondHalfVol = volumes.Skip(count / 2).Average();
            double volumeGrowth = firstHalfVol > 0.0 ? (secondHalfVol - firstHalfVol) / firstHalfVol : 0.0;

            #endregion

            #region 1. Trending Regime Detection

            double trendConfidence = Math.Clamp(trendEfficiency * 150.0, 0.0, 100.0);
            double trendStrength = Math.Clamp(Math.Abs(priceProgress) / avgClose * 2000.0, 0.0, 100.0);
            string trendDirection = priceProgress >= 0.0 ? "Bullish" : "Bearish";
            results["Trending"] = new RegimeClassification(
                "Trending",
                trendConfidence,
                trendStrength,
                $"Price efficiency of {trendEfficiency:F2} over {count} periods. Consolidated {trendDirection} direction."
            );

            #endregion

            #region 2. Range Regime Detection

            // Low efficiency and stable price range implies range bound
            double rangeConfidence = Math.Clamp((1.0 - trendEfficiency) * 100.0, 0.0, 100.0);
            double rangeStrength = Math.Clamp(100.0 - (relStdDev * 1000.0), 0.0, 100.0);
            results["Range"] = new RegimeClassification(
                "Range",
                rangeConfidence,
                rangeStrength,
                $"Trend efficiency is low ({trendEfficiency:F2}) indicating sideways activity. Volatility bounds are stable."
            );

            #endregion

            #region 3. Breakout Regime Detection

            // High volume growth + price near extreme bounds indicates breakout
            var latestCandle = candles[count - 1];
            double priorMax = closes.SkipLast(1).Max();
            double priorMin = closes.SkipLast(1).Min();
            bool isNewExtreme = latestCandle.Close.Value >= priorMax || latestCandle.Close.Value <= priorMin;
            double latestVolumeRatio = avgVolume > 0.0 ? latestCandle.Volume.Value / avgVolume : 1.0;

            double breakoutConfidence = 0.0;
            if (isNewExtreme)
            {
                breakoutConfidence = Math.Clamp(latestVolumeRatio * 40.0 + (volumeGrowth > 0.0 ? 30.0 : 0.0), 10.0, 100.0);
            }
            double breakoutStrength = Math.Clamp(Math.Abs(latestCandle.Close.Value - avgClose) / avgClose * 4000.0, 0.0, 100.0);
            results["Breakout"] = new RegimeClassification(
                "Breakout",
                breakoutConfidence,
                breakoutStrength,
                $"Price closed at extreme point with volume ratio of {latestVolumeRatio:F2} relative to average."
            );

            #endregion

            #region 4. High Volatility & 5. Low Volatility Detection

            double relativeVolScaled = relStdDev * 1000.0; // scale for visualization
            double highVolConfidence = Math.Clamp(relativeVolScaled * 5.0, 0.0, 100.0);
            double highVolStrength = Math.Clamp(relativeVolScaled * 10.0, 0.0, 100.0);
            results["High Volatility"] = new RegimeClassification(
                "High Volatility",
                highVolConfidence,
                highVolStrength,
                $"Relative price standard deviation is {relStdDev:P2}, showing significant dispersion."
            );

            double lowVolConfidence = Math.Clamp(100.0 - (relativeVolScaled * 5.0), 0.0, 100.0);
            double lowVolStrength = Math.Clamp(100.0 - (relativeVolScaled * 10.0), 0.0, 100.0);
            results["Low Volatility"] = new RegimeClassification(
                "Low Volatility",
                lowVolConfidence,
                lowVolStrength,
                $"Relative price standard deviation is extremely low ({relStdDev:P2}), suggesting compression."
            );

            #endregion

            #region 6. Accumulation & 7. Distribution Detection

            // Accumulation: low price progress, high volume in lower half of range
            // Distribution: low price progress, high volume in upper half of range
            double lowestPrice = closes.Min();
            double highestPrice = closes.Max();
            double rangeHeight = highestPrice - lowestPrice;
            double midPrice = lowestPrice + (rangeHeight / 2.0);

            double accWeight = 0.0;
            double distWeight = 0.0;
            for (int i = 0; i < count; i++)
            {
                double price = closes[i];
                double vol = volumes[i];
                if (price < midPrice)
                {
                    accWeight += vol;
                }
                else
                {
                    distWeight += vol;
                }
            }

            double totalVolSum = volumes.Sum();
            double accRatio = totalVolSum > 0.0 ? accWeight / totalVolSum : 0.5;
            double distRatio = totalVolSum > 0.0 ? distWeight / totalVolSum : 0.5;

            double accConfidence = 0.0;
            if (trendEfficiency < 0.25)
            {
                accConfidence = Math.Clamp(accRatio * 100.0, 0.0, 100.0);
            }
            results["Accumulation"] = new RegimeClassification(
                "Accumulation",
                accConfidence,
                Math.Clamp(accRatio * 80.0, 0.0, 100.0),
                $"Quiet accumulation pattern detected with low trend efficiency and {accRatio:P0} volume concentrated at range support."
            );

            double distConfidence = 0.0;
            if (trendEfficiency < 0.25)
            {
                distConfidence = Math.Clamp(distRatio * 100.0, 0.0, 100.0);
            }
            results["Distribution"] = new RegimeClassification(
                "Distribution",
                distConfidence,
                Math.Clamp(distRatio * 80.0, 0.0, 100.0),
                $"Quiet distribution pattern detected with low trend efficiency and {distRatio:P0} volume concentrated at range resistance."
            );

            #endregion

            #region 8. Manipulation Candidate Detection

            // Erratic spikes on very low volume, or extremely high spread relative to normal ATR
            double avgSpreadValue = currentSpreadPoints;
            double latestHighLow = highLows[count - 1];
            double latestVolume = volumes[count - 1];

            double manipConfidence = 0.0;
            if (latestVolume < (avgVolume * 0.3) && latestHighLow > (avgHighLow * 2.0))
            {
                manipConfidence = 85.0;
            }
            else if (avgSpreadValue > (avgHighLow * 1.5))
            {
                manipConfidence = 60.0;
            }

            results["Manipulation Candidate"] = new RegimeClassification(
                "Manipulation Candidate",
                manipConfidence,
                manipConfidence > 0 ? 75.0 : 0.0,
                $"Volatility spike of {latestHighLow:F4} occurring on very low volume ({latestVolume:F0} vs average of {avgVolume:F0})."
            );

            #endregion

            #region 9. Transition Detection

            // Momentum shifts direction while trend efficiency is moderate
            double recentMomentum = endClose - closes[count - 3];
            double priorMomentum = closes[count - 3] - closes[count - 5];
            bool momentumFlipped = (recentMomentum > 0 && priorMomentum < 0) || (recentMomentum < 0 && priorMomentum > 0);

            double transitionConfidence = 0.0;
            if (momentumFlipped && trendEfficiency > 0.2 && trendEfficiency < 0.5)
            {
                transitionConfidence = 75.0;
            }
            else if (trendEfficiency >= 0.4 && trendEfficiency <= 0.6)
            {
                transitionConfidence = 45.0;
            }

            results["Transition"] = new RegimeClassification(
                "Transition",
                transitionConfidence,
                50.0,
                $"Flipped momentum signals or moderate trend efficiency suggest an active structural transition."
            );

            #endregion

            return results;
        }

        /// <summary>
        /// Selects the dominant regime (the regime with the highest confidence level).
        /// </summary>
        public RegimeClassification GetDominantRegime(IReadOnlyDictionary<string, RegimeClassification> classifications)
        {
            if (classifications == null || classifications.Count == 0)
                return new RegimeClassification("Unknown", 100.0, 50.0, "No classifications supplied.");

            return classifications.Values.OrderByDescending(r => r.Confidence).First();
        }
    }
}
