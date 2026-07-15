using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public sealed class ExperienceCollector : IExperienceCollector
    {
        private static readonly TimeSpan MaxSampleRetention = TimeSpan.FromDays(1);
        private readonly ConcurrentDictionary<Guid, ExperienceSample> _activeSamples = new();
        private readonly IExperienceDatabaseWriter _dbWriter;
        private readonly ILogger<ExperienceCollector> _logger;

        public ExperienceCollector(IExperienceDatabaseWriter dbWriter, ILogger<ExperienceCollector> logger)
        {
            _dbWriter = dbWriter ?? throw new ArgumentNullException(nameof(dbWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RecordDecision(ExperienceSample sample)
        {
            if (sample == null)
            {
                throw new ArgumentNullException(nameof(sample));
            }

            // Evict expired samples to prevent memory leaks
            EvictStaleSamples();

            _activeSamples[sample.Id] = sample;

            // Map and persist initial ExperienceRecord using the correlated sample.Id
            var record = new ExperienceRecord(
                sample.Id,
                sample.Symbol,
                sample.FeatureVector,
                "Stockfish-v1.0",
                sample.Decision.Action == DecisionAction.BUY ? 0.8 : 0.2,
                sample.Decision.Action == DecisionAction.SELL ? 0.8 : 0.2,
                sample.MarketStateSnapshot.Risk,
                sample.MarketRegimeLabel,
                sample.Decision.Action.ToString()
            );

            _dbWriter.Enqueue(record);
            _logger.LogInformation("[ExperienceCollector] Recorded initial decision sample {Id} for symbol {Symbol} ({Action}).", sample.Id, sample.Symbol, sample.Decision.Action);
        }

        public Task UpdateOutcomeAsync(
            Guid sampleId,
            double exitPrice,
            double maxDrawdown,
            double holdingTimeMinutes,
            double outcomeScore,
            string mistakeClassification)
        {
            if (_activeSamples.TryRemove(sampleId, out var sample))
            {
                sample.ExitPrice = exitPrice;
                sample.MaxDrawdown = maxDrawdown;
                sample.HoldingTimeMinutes = holdingTimeMinutes;
                sample.OutcomeScore = outcomeScore;
                sample.MistakeClassification = mistakeClassification ?? "None";

                _logger.LogInformation(
                    "[ExperienceCollector] Resolved outcome for sample {Id}. ExitPrice: {ExitPrice}, MaxDD: {MaxDD:F1} pips, Score: {Score:F2}, Mistake: {Mistake}",
                    sampleId, exitPrice, maxDrawdown, outcomeScore, sample.MistakeClassification);

                // Enqueue the updated/completed ExperienceRecord using the SAME correlated sampleId
                var completedRecord = new ExperienceRecord(
                    sample.Id,
                    sample.Symbol,
                    sample.FeatureVector,
                    "Stockfish-v1.0",
                    sample.Decision.Action == DecisionAction.BUY ? 0.8 : 0.2,
                    sample.Decision.Action == DecisionAction.SELL ? 0.8 : 0.2,
                    sample.MarketStateSnapshot.Risk,
                    sample.MarketRegimeLabel,
                    sample.Decision.Action.ToString()
                )
                {
                    RealizedPips = outcomeScore * 100.0, // translate score to pips for db
                    IsCompleted = true
                };

                _dbWriter.Enqueue(completedRecord);
            }
            else
            {
                _logger.LogWarning("[ExperienceCollector] Attempted to update outcome for untracked sample {Id}.", sampleId);
            }

            return Task.CompletedTask;
        }

        private void EvictStaleSamples()
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                var staleKeys = _activeSamples
                    .Where(kvp => now - kvp.Value.TimestampUtc > MaxSampleRetention)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in staleKeys)
                {
                    if (_activeSamples.TryRemove(key, out _))
                    {
                        _logger.LogDebug("[ExperienceCollector] Evicted stale unresolved experience sample {Id} due to retention timeout.", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExperienceCollector] Error occurred during stale experience sample eviction.");
            }
        }
    }
}
