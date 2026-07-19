using Microsoft.Extensions.Logging;
using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Enums;
using Nexus.Core.AI.Interfaces;

namespace Nexus.Application.AI.Evaluation
{
    /// <summary>
    /// Executes the strict promotion policy for AI models.
    /// Evaluates multiple thresholds; only if all pass, a Candidate replaces the Champion.
    /// </summary>
    public class ChampionChallengerEvaluator
    {
        private readonly IModelRegistry _modelRegistry;
        private readonly IExperimentTracker _experimentTracker;
        private readonly ILogger<ChampionChallengerEvaluator> _logger;

        // Strict institutional promotion thresholds
        private const double MinWinRate = 0.55;
        private const double MinProfitFactor = 1.5;
        private const double MaxDrawdownThreshold = 0.05;

        public ChampionChallengerEvaluator(
            IModelRegistry modelRegistry,
            IExperimentTracker experimentTracker,
            ILogger<ChampionChallengerEvaluator> logger)
        {
            _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
            _experimentTracker = experimentTracker ?? throw new ArgumentNullException(nameof(experimentTracker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EvaluateCandidatesAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Starting Champion vs Challenger evaluation cycle.");

            var currentChampion = await _modelRegistry.GetChampionAsync(ct);
            var candidates = await _modelRegistry.GetCandidatesAsync(ct);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("No candidate models available for evaluation.");
                return;
            }

            foreach (var candidate in candidates)
            {
                bool isPromoted = await EvaluateSingleCandidateAsync(currentChampion, candidate, ct);

                if (isPromoted)
                {
                    _logger.LogInformation("Model {ModelId} promoted to CHAMPION.", candidate.ModelId);

                    // Demote old champion to Archive
                    if (currentChampion != null)
                    {
                        await _modelRegistry.UpdateModelStatusAsync(currentChampion.ModelId, ModelStatus.Archived, ct);
                    }

                    // Promote new champion
                    await _modelRegistry.UpdateModelStatusAsync(candidate.ModelId, ModelStatus.Champion, ct);

                    // Only one champion can be promoted per cycle
                    break;
                }
                else
                {
                    _logger.LogInformation("Model {ModelId} failed promotion. Marked as Rejected.", candidate.ModelId);
                    await _modelRegistry.UpdateModelStatusAsync(candidate.ModelId, ModelStatus.Rejected, ct);
                }
            }
        }

        private async Task<bool> EvaluateSingleCandidateAsync(ModelMetadata? champion, ModelMetadata candidate, CancellationToken ct)
        {
            var exp = await _experimentTracker.GetExperimentAsync(candidate.ExperimentId, ct);
            if (exp == null) return false;

            var metrics = exp.FinalMetrics;

            double winRate = metrics.GetValueOrDefault("WinRate", 0);
            double profitFactor = metrics.GetValueOrDefault("ProfitFactor", 0);
            double maxDrawdown = metrics.GetValueOrDefault("MaxDrawdown", 1.0);
            double validationLoss = metrics.GetValueOrDefault("ValidationLoss", double.MaxValue);

            // 1. Absolute Threshold Checks
            if (winRate < MinWinRate || profitFactor < MinProfitFactor || maxDrawdown > MaxDrawdownThreshold)
            {
                return false;
            }

            // 2. Relative Check against current Champion
            if (champion != null)
            {
                var champExp = await _experimentTracker.GetExperimentAsync(champion.ExperimentId, ct);
                if (champExp != null)
                {
                    double champWinRate = champExp.FinalMetrics.GetValueOrDefault("WinRate", 0);
                    double champLoss = champExp.FinalMetrics.GetValueOrDefault("ValidationLoss", double.MaxValue);

                    // Candidate must be strictly better in loss AND at least equal in WinRate
                    if (validationLoss >= champLoss || winRate < champWinRate)
                    {
                        return false;
                    }
                }
            }

            return true; // Passed all gates
        }
    }
}