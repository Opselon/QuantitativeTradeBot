using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public class CurrencyStrengthEngine : ICurrencyStrengthEngine
    {
        private readonly ConcurrentDictionary<string, double> _baselines = new();
        private readonly ConcurrentDictionary<string, double> _currentPrices = new();
        private readonly ConcurrentDictionary<string, double> _strengths = new();

        private static readonly HashSet<string> MonitoredPairs = new(StringComparer.OrdinalIgnoreCase)
        {
            "EURUSD", "GBPUSD", "USDJPY", "USDCHF", "USDCAD", "AUDUSD", "NZDUSD"
        };

        private static readonly string[] Currencies = { "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD", "NZD" };

        public CurrencyStrengthEngine()
        {
            foreach (var cur in Currencies)
            {
                _strengths[cur] = 50.0; // neutral initial state
            }
        }

        public double GetStrengthScore(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency)) return 50.0;
            var upper = currency.ToUpperInvariant();
            return _strengths.TryGetValue(upper, out double score) ? score : 50.0;
        }

        public IReadOnlyDictionary<string, double> GetAllStrengthScores()
        {
            return _strengths;
        }

        public void UpdateFromTick(Tick tick)
        {
            var pair = tick.Symbol.Name.ToUpperInvariant();
            if (!MonitoredPairs.Contains(pair)) return;

            double midPrice = (tick.Bid + tick.Ask) / 2.0;
            if (midPrice <= 0) return;

            // Initialize baseline on first tick
            _baselines.TryAdd(pair, midPrice);
            _currentPrices[pair] = midPrice;

            RecalculateScores();
        }

        private void RecalculateScores()
        {
            // Reset aggregate performance
            var performances = new Dictionary<string, List<double>>();
            foreach (var cur in Currencies)
            {
                performances[cur] = new List<double>();
            }

            foreach (var pair in MonitoredPairs)
            {
                if (!_baselines.TryGetValue(pair, out double baseline) ||
                    !_currentPrices.TryGetValue(pair, out double current))
                {
                    continue;
                }

                double pctChange = (current - baseline) / baseline;

                // Split pair name, e.g. "EURUSD" -> base: "EUR", quote: "USD"
                string baseCur = pair.Substring(0, 3);
                string quoteCur = pair.Substring(3, 3);

                // Base currency gets pctChange positive impact
                performances[baseCur].Add(pctChange);
                // Quote currency gets pctChange negative impact
                performances[quoteCur].Add(-pctChange);
            }

            // Compute average percentage change for each currency and map to a 0-100 score
            var rawScores = new Dictionary<string, double>();
            double maxScore = double.MinValue;
            double minScore = double.MaxValue;

            foreach (var cur in Currencies)
            {
                var list = performances[cur];
                double avgPerformance = list.Count > 0 ? System.Linq.Enumerable.Average(list) : 0.0;
                rawScores[cur] = avgPerformance;

                if (avgPerformance > maxScore) maxScore = avgPerformance;
                if (avgPerformance < minScore) minScore = avgPerformance;
            }

            // Map raw scores linearly to 0 - 100 range
            double range = maxScore - minScore;
            foreach (var cur in Currencies)
            {
                double score = 50.0;
                if (range > 0.00001)
                {
                    score = 10.0 + ((rawScores[cur] - minScore) / range) * 80.0; // bounded to 10 - 90 for stability
                }
                _strengths[cur] = Math.Clamp(Math.Round(score, 1), 0.0, 100.0);
            }
        }
    }
}
