using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.MarketIntelligence.MultiTimeframe;

namespace Nexus.MarketIntelligence.Features
{
    /// <summary>
    /// Extract deterministic quantitative features from market states for neural network input and decision analytics.
    /// Features are categorized and stable.
    /// </summary>
    public sealed class FeatureExtractor
    {
        /// <summary>
        /// Extracts features from candles, ticks, session, and multi-timeframe states.
        /// </summary>
        public ExtractedFeatures Extract(
            string symbol,
            IReadOnlyList<Candle> candles,
            Tick latestTick,
            MarketSession currentSession,
            MultiTimeframeState? mtfState)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));

            var dict = new Dictionary<string, double>(StringComparer.Ordinal);
            var timestamp = DateTime.UtcNow;

            #region 1. Time Features (Cyclical hourly & weekly)

            dict["time_hour_sin"] = Math.Sin(timestamp.Hour * 2.0 * Math.PI / 24.0);
            dict["time_hour_cos"] = Math.Cos(timestamp.Hour * 2.0 * Math.PI / 24.0);
            dict["time_day_of_week"] = (double)timestamp.DayOfWeek / 7.0;

            #endregion

            #region 2. Session Features

            bool isTokyo = currentSession?.Name.Contains("Tokyo", StringComparison.OrdinalIgnoreCase) == true;
            bool isLondon = currentSession?.Name.Contains("London", StringComparison.OrdinalIgnoreCase) == true;
            bool isNewYork = currentSession?.Name.Contains("New York", StringComparison.OrdinalIgnoreCase) == true;

            dict["session_tokyo_active"] = isTokyo ? 1.0 : 0.0;
            dict["session_london_active"] = isLondon ? 1.0 : 0.0;
            dict["session_newyork_active"] = isNewYork ? 1.0 : 0.0;

            #endregion

            #region Fallback Safeguard for Candles

            if (candles == null || candles.Count < 5)
            {
                // Write baseline fallback values for all standard categories
                dict["trend_sma_ratio_5"] = 0.0;
                dict["trend_sma_ratio_20"] = 0.0;
                dict["trend_slope"] = 0.0;

                dict["momentum_rsi_approx"] = 0.5;
                dict["momentum_roc_3"] = 0.0;

                dict["volatility_relative"] = 0.15;
                dict["volatility_parkinson"] = 0.15;

                dict["liquidity_volume_ratio"] = 1.0;
                dict["liquidity_spread_points"] = latestTick.Spread > 0 ? latestTick.Spread : 2.0;

                dict["structure_distance_to_high"] = 0.0;
                dict["structure_distance_to_low"] = 0.0;

                dict["cross_trend_alignment_score"] = 50.0;
                dict["cross_momentum_alignment_score"] = 50.0;

                return new ExtractedFeatures(symbol, timestamp, dict);
            }

            #endregion

            #region 3. Trend Features

            int count = candles.Count;
            var closes = candles.Select(c => c.Close.Value).ToList();
            double latestClose = closes[count - 1];

            // SMA 5
            double sum5 = 0.0;
            int p5 = Math.Min(5, count);
            for (int i = count - p5; i < count; i++) sum5 += closes[i];
            double sma5 = sum5 / p5;
            dict["trend_sma_ratio_5"] = sma5 > 0 ? (latestClose - sma5) / sma5 : 0.0;

            // SMA 20
            double sum20 = 0.0;
            int p20 = Math.Min(20, count);
            for (int i = count - p20; i < count; i++) sum20 += closes[i];
            double sma20 = sum20 / p20;
            dict["trend_sma_ratio_20"] = sma20 > 0 ? (latestClose - sma20) / sma20 : 0.0;

            // Trend slope over last 5 periods
            double slope = (closes[count - 1] - closes[count - p5]) / p5;
            dict["trend_slope"] = latestClose > 0 ? slope / latestClose * 100.0 : 0.0;

            #endregion

            #region 4. Momentum Features

            // RSI approximation based on last 5 bars change ratio
            double gains = 0.0;
            double losses = 0.0;
            for (int i = count - p5 + 1; i < count; i++)
            {
                double change = closes[i] - closes[i - 1];
                if (change > 0) gains += change;
                else losses += Math.Abs(change);
            }
            double rsiApprox = gains + losses > 0 ? gains / (gains + losses) : 0.5;
            dict["momentum_rsi_approx"] = rsiApprox;

            // ROC 3
            double startROC = closes[count - Math.Min(3, count)];
            dict["momentum_roc_3"] = startROC > 0 ? (latestClose - startROC) / startROC : 0.0;

            #endregion

            #region 5. Volatility Features

            // Close standard deviation relative to SMA20
            double mean = sum20 / p20;
            double sumSq = 0.0;
            for (int i = count - p20; i < count; i++)
            {
                sumSq += Math.Pow(closes[i] - mean, 2);
            }
            double stdDev = Math.Sqrt(sumSq / p20);
            dict["volatility_relative"] = mean > 0 ? stdDev / mean : 0.15;

            // Parkinson Volatility Approximation (using high/low ranges)
            double sumHighLowLog = 0.0;
            for (int i = count - p5; i < count; i++)
            {
                double h = candles[i].High.Value;
                double l = Math.Max(1e-9, candles[i].Low.Value);
                sumHighLowLog += Math.Pow(Math.Log(h / l), 2);
            }
            double parkinson = Math.Sqrt((1.0 / (4.0 * p5 * Math.Log(2.0))) * sumHighLowLog);
            dict["volatility_parkinson"] = parkinson;

            #endregion

            #region 6. Liquidity Features

            double latestVol = candles[count - 1].Volume.Value;
            double avgVol = candles.Average(c => c.Volume.Value);
            dict["liquidity_volume_ratio"] = avgVol > 0 ? latestVol / avgVol : 1.0;
            dict["liquidity_spread_points"] = latestTick.Spread > 0 ? latestTick.Spread : 2.0;

            #endregion

            #region 7. Structure Features

            double maxHigh = candles.Max(c => c.High.Value);
            double minLow = candles.Min(c => c.Low.Value);

            dict["structure_distance_to_high"] = maxHigh > 0 ? (maxHigh - latestClose) / maxHigh : 0.0;
            dict["structure_distance_to_low"] = latestClose > 0 ? (latestClose - minLow) / latestClose : 0.0;

            #endregion

            #region 8. Cross-Timeframe Features

            if (mtfState != null)
            {
                // Trend alignment score mapped to range 0-100
                double alignmentTrend = mtfState.TrendAlignment switch
                {
                    "Bullish" => 100.0,
                    "Bearish" => 0.0,
                    _ => 50.0
                };
                dict["cross_trend_alignment_score"] = alignmentTrend;

                // Momentum alignment score mapped to range 0-100
                double alignmentMom = mtfState.MomentumAlignment switch
                {
                    "Bullish" => 100.0,
                    "Bearish" => 0.0,
                    _ => 50.0
                };
                dict["cross_momentum_alignment_score"] = alignmentMom;
            }
            else
            {
                dict["cross_trend_alignment_score"] = 50.0;
                dict["cross_momentum_alignment_score"] = 50.0;
            }

            #endregion

            return new ExtractedFeatures(symbol, timestamp, dict);
        }
    }
}
