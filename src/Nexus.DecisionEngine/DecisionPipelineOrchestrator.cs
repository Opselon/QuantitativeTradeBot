using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Service contract for orchestrating the end-to-end decision-making pipeline:
    /// Market Snapshot -> Feature Evaluation -> Model Evaluation -> Scenario Generation
    /// -> Scenario Scoring -> Risk Evaluation -> Decision Ranking -> Final Decision -> Execution Request.
    /// </summary>
    public interface IDecisionPipelineOrchestrator
    {
        Task<DecisionPackage> OrchestrateDecisionAsync(
            MarketState marketState,
            RiskState riskState,
            float[] featureVector,
            double neuralBuyConfidence,
            double neuralSellConfidence,
            CancellationToken ct);
    }

    public sealed class DecisionPipelineOrchestrator : IDecisionPipelineOrchestrator
    {
        private readonly IMultiModelConsensusAggregator _consensusAggregator;
        private readonly IMarketHypothesisEngine _hypothesisEngine;
        private readonly IDecisionScenarioSearchEngine _scenarioSearchEngine;
        private readonly IUncertaintyEngine _uncertaintyEngine;
        private readonly IMarketMemory _marketMemory;

        public DecisionPipelineOrchestrator(
            IMultiModelConsensusAggregator consensusAggregator,
            IMarketHypothesisEngine hypothesisEngine,
            IDecisionScenarioSearchEngine scenarioSearchEngine,
            IUncertaintyEngine uncertaintyEngine,
            IMarketMemory marketMemory)
        {
            _consensusAggregator = consensusAggregator ?? throw new ArgumentNullException(nameof(consensusAggregator));
            _hypothesisEngine = hypothesisEngine ?? throw new ArgumentNullException(nameof(hypothesisEngine));
            _scenarioSearchEngine = scenarioSearchEngine ?? throw new ArgumentNullException(nameof(scenarioSearchEngine));
            _uncertaintyEngine = uncertaintyEngine ?? throw new ArgumentNullException(nameof(uncertaintyEngine));
            _marketMemory = marketMemory ?? throw new ArgumentNullException(nameof(marketMemory));
        }

        public async Task<DecisionPackage> OrchestrateDecisionAsync(
            MarketState marketState,
            RiskState riskState,
            float[] featureVector,
            double neuralBuyConfidence,
            double neuralSellConfidence,
            CancellationToken ct)
        {
            if (marketState == null) throw new ArgumentNullException(nameof(marketState));
            if (riskState == null) throw new ArgumentNullException(nameof(riskState));

            // Stage 1: Market Snapshot
            // Capture incoming market state parameters (already provided in marketState object)

            // Stage 2: Feature Evaluation (from feature vector or similar patterns)
            double successRate = await _marketMemory.GetSimilarSituationsSuccessRateAsync(marketState.Symbol, featureVector?.Select(f => (double)f).ToArray() ?? Array.Empty<double>(), ct);

            // Stage 3: Model Evaluation (Multi-Model Consensus)
            var consensus = await _consensusAggregator.AggregateConsensusAsync(marketState, ct);

            // Stage 4: Scenario Generation (Hypotheses)
            var hypotheses = _hypothesisEngine.GenerateHypotheses(marketState, neuralBuyConfidence, neuralSellConfidence);
            var bestHypothesis = hypotheses.OrderByDescending(h => h.GetUtility()).First();

            // Stage 5 & 6: Scenario Scoring & Search
            var searchResult = await _scenarioSearchEngine.SearchBestActionAsync(marketState, riskState, neuralBuyConfidence, neuralSellConfidence, ct);

            // Stage 7: Risk Evaluation & Uncertainty Checks
            var uncertainty = _uncertaintyEngine.EvaluateUncertainty(marketState, Math.Max(neuralBuyConfidence, neuralSellConfidence), consensus.AggregatedConfidence);

            // Stage 8: Decision Ranking / Action Selection
            // Map the search result's best action
            DecisionAction selectedAction = searchResult.Action;

            // Uncertainty override: prefer WAIT if uncertainty is High
            if (uncertainty == UncertaintyLevel.High && selectedAction != DecisionAction.CLOSE && selectedAction != DecisionAction.REDUCE)
            {
                selectedAction = DecisionAction.WAIT;
            }

            // Stage 9: Final Decision & Execution Request Preparation
            double finalConfidence = consensus.AggregatedConfidence * (1.0 - (uncertainty == UncertaintyLevel.High ? 0.5 : (uncertainty == UncertaintyLevel.Medium ? 0.2 : 0.0)));
            finalConfidence = Math.Clamp(finalConfidence, 0.0, 1.0);

            var alternatives = new Dictionary<DecisionAction, double>
            {
                { DecisionAction.BUY, neuralBuyConfidence },
                { DecisionAction.SELL, neuralSellConfidence },
                { DecisionAction.WAIT, 1.0 - Math.Max(neuralBuyConfidence, neuralSellConfidence) }
            };

            bool isExecutionReady = !riskState.IsTradingBlocked && selectedAction != DecisionAction.WAIT;

            string evidence = $"Consensus Dominant Bias: {consensus.DominantBias} (Score: {consensus.AggregatedScore:F2}). " +
                               $"Best Hypothesis: {bestHypothesis.Name} (Utility: {bestHypothesis.GetUtility():F1}). " +
                               $"Historical Match Success Rate: {successRate:P0}. " +
                               $"Uncertainty Level: {uncertainty}.";

            string riskSummary = $"Risk Limit Blocked: {riskState.IsTradingBlocked}. " +
                                 $"Max Drawdown Projection: {searchResult.MaxDrawdown:F1} pips. " +
                                 $"Stop Loss Probability: {searchResult.ProbabilityOfStopLoss:P0}.";

            string expectedOutcome = $"Expected Value: {searchResult.ExpectedValue:F1} pips. " +
                                     $"Probability of Take Profit: {searchResult.ProbabilityOfTakeProfit:P0}. " +
                                     $"Estimated Resolution Time: {searchResult.TimeToResolutionMinutes} minutes.";

            return new DecisionPackage(
                selectedAction,
                finalConfidence,
                evidence,
                alternatives,
                riskSummary,
                expectedOutcome,
                isExecutionReady
            );
        }
    }
}
