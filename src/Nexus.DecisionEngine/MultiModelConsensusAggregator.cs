using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Service contract for aggregating multiple independent model evaluators.
    /// </summary>
    public interface IMultiModelConsensusAggregator
    {
        Task<ConsensusStatePackage> AggregateConsensusAsync(MarketState marketState, CancellationToken ct);
    }

    public sealed class ConsensusStatePackage
    {
        public double AggregatedScore { get; }      // -1.0 to 1.0
        public double AggregatedConfidence { get; } // 0.0 to 1.0
        public List<string> ContributorSummaries { get; }
        public string DominantBias { get; }

        public ConsensusStatePackage(double score, double confidence, List<string> contributorSummaries, string dominantBias)
        {
            AggregatedScore = score;
            AggregatedConfidence = confidence;
            ContributorSummaries = contributorSummaries ?? new List<string>();
            DominantBias = dominantBias ?? "Neutral";
        }
    }

    public sealed class MultiModelConsensusAggregator : IMultiModelConsensusAggregator
    {
        private readonly List<IModelEvaluator> _evaluators;

        public MultiModelConsensusAggregator(IEnumerable<IModelEvaluator> evaluators)
        {
            _evaluators = evaluators?.ToList() ?? new List<IModelEvaluator>();
        }

        public async Task<ConsensusStatePackage> AggregateConsensusAsync(MarketState marketState, CancellationToken ct)
        {
            if (marketState == null)
            {
                throw new ArgumentNullException(nameof(marketState));
            }

            if (!_evaluators.Any())
            {
                return new ConsensusStatePackage(0.0, 0.0, new List<string> { "No evaluators configured." }, "Neutral");
            }

            double totalScore = 0.0;
            double totalConfidence = 0.0;
            double weightSum = 0.0;
            var summaries = new List<string>();

            foreach (var evaluator in _evaluators)
            {
                ct.ThrowIfCancellationRequested();

                var res = await evaluator.EvaluateAsync(marketState, ct);

                // Models with higher confidence have higher weights in the consensus
                double weight = Math.Max(0.1, res.Confidence);
                totalScore += res.Score * weight;
                totalConfidence += res.Confidence * weight;
                weightSum += weight;

                summaries.Add($"{evaluator.Name}: Score={res.Score:F2}, Conf={res.Confidence:P0}. Reason: {res.Explanation}");
            }

            double aggregatedScore = weightSum > 0 ? (totalScore / weightSum) : 0.0;
            double aggregatedConfidence = weightSum > 0 ? (totalConfidence / weightSum) : 0.0;

            string dominantBias = "Neutral";
            if (aggregatedScore > 0.25)
            {
                dominantBias = "Bullish";
            }
            else if (aggregatedScore < -0.25)
            {
                dominantBias = "Bearish";
            }

            return new ConsensusStatePackage(aggregatedScore, aggregatedConfidence, summaries, dominantBias);
        }
    }
}
