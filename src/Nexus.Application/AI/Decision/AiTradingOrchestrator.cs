using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Application.AI.Features;
using Nexus.Core.AI.Interfaces;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.AI.Decision
{
    /// <summary>
    /// The apex orchestrator for the live trading loop.
    /// Ingests live market states, generates feature tensors, queries the Inference Engine,
    /// and uses the Decision Fusion Engine to dispatch safe trades.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Triggered by: src/Nexus.Desktop/ViewModels/Workspaces/DashboardViewModel.cs (on every live tick)
    /// - Consumes: src/Nexus.Application/AI/Decision/DecisionFusionEngine.cs (to merge rules and risk)
    /// </remarks>
    public class AiTradingOrchestrator
    {
        #region Private Fields & Dependencies
        private readonly FeatureOrchestrator _featureOrchestrator;
        private readonly IInferenceEngine _inferenceEngine;
        private readonly DecisionFusionEngine _fusionEngine;
        private readonly IDecisionEventStream _decisionEventStream;
        private readonly ILogger<AiTradingOrchestrator> _logger;
        #endregion

        #region Constructor
        public AiTradingOrchestrator(
            FeatureOrchestrator featureOrchestrator,
            IInferenceEngine inferenceEngine,
            DecisionFusionEngine fusionEngine,
            IDecisionEventStream decisionEventStream,
            ILogger<AiTradingOrchestrator> logger)
        {
            _featureOrchestrator = featureOrchestrator ?? throw new ArgumentNullException(nameof(featureOrchestrator));
            _inferenceEngine = inferenceEngine ?? throw new ArgumentNullException(nameof(inferenceEngine));
            _fusionEngine = fusionEngine ?? throw new ArgumentNullException(nameof(fusionEngine));
            _decisionEventStream = decisionEventStream ?? throw new ArgumentNullException(nameof(decisionEventStream));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Live Market Evaluation
        /// <summary>
        /// Evaluates a live tick and dispatches a trading decision if favorable.
        /// </summary>
        public async Task EvaluateLiveMarketAsync(
            MarketState currentState,
            IReadOnlyList<Candle> recentCandles,
            IReadOnlyList<Tick> recentTicks,
            ConsensusState consensusState,
            RiskState riskState,
            CancellationToken ct = default)
        {
            try
            {
                // 1. Feature Engineering: Convert raw live data into the 64-element float vector expected by TorchSharp
                double[] features = _featureOrchestrator.GenerateFeatureVector(currentState, recentCandles, recentTicks);

                // 2. Inference: Run the neural network forward pass (or safe fallbacks if awaiting training)
                var prediction = await _inferenceEngine.PredictAsync(features, ct);

                // 3. Decision Fusion: Combine neural network predictions with strict technical and risk constraints
                TradeDecision finalDecision = _fusionEngine.Fuse(prediction, consensusState, riskState);

                _logger.LogInformation(
                    "[FUSION] Symbol: {Symbol} | Action: {Action} | Conf: {Confidence:P1} | Reason: {Reason}",
                    currentState.Symbol, finalDecision.Action, prediction.Confidence, finalDecision.Reason);

                // 4. Dispatch the decision to the Execution Pipeline Event Stream
                // REASON: Always publish the decision created event, even for WAIT actions.
                // This ensures the live UI Dashboard, Explainability Timeline and console logs are fully updated.
                // Extracts the live probabilities from TorchSharp to scale the Stockfish Expected Utility progress bars.
                double buyProb = prediction.Probabilities.GetValueOrDefault("Buy", 0.33);
                double sellProb = prediction.Probabilities.GetValueOrDefault("Sell", 0.33);
                double waitProb = prediction.Probabilities.GetValueOrDefault("Wait", 0.34);

                _decisionEventStream.PublishDecisionCreated(new Core.DomainEvents.DecisionCreatedEvent(
                    decisionId: Guid.NewGuid(),
                    symbol: currentState.Symbol,
                    action: finalDecision.Action.ToString(),
                    confidence: prediction.Confidence,
                    reason: finalDecision.Reason,
                    buyUtility: buyProb * 10.0,   // Mapped scaled buy expected utility
                    sellUtility: sellProb * -10.0, // Sell scaled negatively for symmetrical progress representations
                    waitUtility: waitProb * 5.0
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ORCHESTRATOR ERROR] AI execution pipeline failed.");
            }
        }
        #endregion
    }
}