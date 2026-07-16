using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Entities;

namespace Nexus.Training
{
    public enum TimeframeLearningCategory
    {
        Scalping,
        Intraday,
        Swing
    }

    /// <summary>
    /// Tracks performance metrics specifically for one timeframe learning category.
    /// </summary>
    public sealed class CategoryMetrics
    {
        public int TotalSamples { get; set; }
        public int ProfitableCount { get; set; }
        public double AverageReward { get; set; }
        public double WinRate => TotalSamples > 0 ? (double)ProfitableCount / TotalSamples : 0.0;
        public double ProfitFactor { get; set; } = 1.0;
        public double MaxDrawdown { get; set; }
    }

    /// <summary>
    /// Separates quantitative data, models, and metric evaluations into isolated, timeframe-specific learning paths.
    /// Strictly prevents mixing scalping (fast feedback) with swing trading (slow feedback) behaviors.
    /// </summary>
    public sealed class TimeframeLearningManager
    {
        private readonly ConcurrentDictionary<TimeframeLearningCategory, List<ExperienceSample>> _datasets = new();
        private readonly ConcurrentDictionary<TimeframeLearningCategory, CategoryMetrics> _metrics = new();
        private readonly RewardEvaluator _rewardEvaluator = new();

        public TimeframeLearningManager()
        {
            foreach (TimeframeLearningCategory category in Enum.GetValues(typeof(TimeframeLearningCategory)))
            {
                _datasets[category] = new List<ExperienceSample>();
                _metrics[category] = new CategoryMetrics();
            }
        }

        /// <summary>
        /// Automatically maps a TimeframeInterval to its corresponding learning category.
        /// </summary>
        public static TimeframeLearningCategory GetCategoryForTimeframe(TimeframeInterval timeframe)
        {
            return timeframe switch
            {
                TimeframeInterval.M1 => TimeframeLearningCategory.Scalping,
                TimeframeInterval.M5 => TimeframeLearningCategory.Scalping,
                TimeframeInterval.M15 => TimeframeLearningCategory.Scalping,
                TimeframeInterval.M30 => TimeframeLearningCategory.Intraday,
                TimeframeInterval.H1 => TimeframeLearningCategory.Intraday,
                TimeframeInterval.H4 => TimeframeLearningCategory.Swing,
                TimeframeInterval.D1 => TimeframeLearningCategory.Swing,
                _ => throw new ArgumentOutOfRangeException(nameof(timeframe), $"Unknown timeframe interval: {timeframe}")
            };
        }

        /// <summary>
        /// Registers an experience sample into its appropriate timeframe learning category dataset.
        /// </summary>
        public void RegisterExperience(ExperienceSample sample)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));

            var category = GetCategoryForTimeframe(sample.Timeframe);
            lock (_datasets[category])
            {
                _datasets[category].Add(sample);
            }

            RecalculateMetrics(category);
        }

        /// <summary>
        /// Registers a batch of experience samples.
        /// </summary>
        public void RegisterExperiences(IEnumerable<ExperienceSample> samples)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));

            foreach (var sample in samples)
            {
                RegisterExperience(sample);
            }
        }

        /// <summary>
        /// Retrieves the isolated dataset for a specific timeframe category.
        /// </summary>
        public IReadOnlyList<ExperienceSample> GetDataset(TimeframeLearningCategory category)
        {
            lock (_datasets[category])
            {
                return _datasets[category].ToList();
            }
        }

        /// <summary>
        /// Retrieves the evaluation performance metrics for a specific timeframe category.
        /// </summary>
        public CategoryMetrics GetMetrics(TimeframeLearningCategory category)
        {
            return _metrics[category];
        }

        private void RecalculateMetrics(TimeframeLearningCategory category)
        {
            List<ExperienceSample> samples;
            lock (_datasets[category])
            {
                samples = _datasets[category].ToList();
            }

            if (samples.Count == 0) return;

            double totalReward = 0.0;
            double totalGains = 0.0;
            double totalLosses = 0.0;
            double maxDrawdown = 0.0;
            int profitable = 0;

            foreach (var s in samples)
            {
                var rewardBreakdown = _rewardEvaluator.Evaluate(s);
                totalReward += rewardBreakdown.TotalReward;
                maxDrawdown = Math.Max(maxDrawdown, s.MaxDrawdown);

                if (s.Result > 0.0)
                {
                    profitable++;
                    totalGains += s.Result;
                }
                else
                {
                    totalLosses += Math.Abs(s.Result);
                }
            }

            var metrics = _metrics[category];
            metrics.TotalSamples = samples.Count;
            metrics.ProfitableCount = profitable;
            metrics.AverageReward = totalReward / samples.Count;
            metrics.MaxDrawdown = maxDrawdown;
            metrics.ProfitFactor = totalLosses > 0.0 ? totalGains / totalLosses : (totalGains > 0.0 ? 10.0 : 1.0);
        }

        /// <summary>
        /// Clears all stored datasets and resets metrics.
        /// </summary>
        public void Clear()
        {
            foreach (var key in _datasets.Keys)
            {
                lock (_datasets[key])
                {
                    _datasets[key].Clear();
                }
                var m = _metrics[key];
                m.TotalSamples = 0;
                m.ProfitableCount = 0;
                m.AverageReward = 0.0;
                m.ProfitFactor = 1.0;
                m.MaxDrawdown = 0.0;
            }
        }
    }
}
