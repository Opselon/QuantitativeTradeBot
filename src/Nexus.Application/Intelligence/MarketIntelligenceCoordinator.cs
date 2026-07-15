using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Application.Ports;

namespace Nexus.Application.Intelligence
{
    /// <summary>
    /// Manages the real-time high-performance AI state machine loop.
    /// Interops with the C++20 Core, runs ONNX neural evaluations, and streams datasets to the Database.
    /// </summary>
    public class MarketIntelligenceCoordinator : IDisposable
    {
        private readonly INativeCoreService _nativeCore;
        private readonly INeuralModelService _neuralModel;
        private readonly IDecisionEngine _decisionEngine;
        private readonly IExperienceDatabaseWriter _dbWriter; // Port interface
        private readonly ILogger<MarketIntelligenceCoordinator> _logger;

        public event Action<EvaluationResult, TradeDecision>? OnAnalysisCompleted;

        public MarketIntelligenceCoordinator(
            INativeCoreService nativeCore,
            INeuralModelService neuralModel,
            IDecisionEngine decisionEngine,
            IExperienceDatabaseWriter dbWriter,
            ILogger<MarketIntelligenceCoordinator> logger)
        {
            _nativeCore = nativeCore ?? throw new ArgumentNullException(nameof(nativeCore));
            _neuralModel = neuralModel ?? throw new ArgumentNullException(nameof(neuralModel));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _dbWriter = dbWriter ?? throw new ArgumentNullException(nameof(dbWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main high-performance pipeline entry point. Triggered when a new tick is received.
        /// </summary>
        public async Task ProcessTickAsync(Tick tick, RiskState currentRiskState, CancellationToken ct)
        {
            try
            {
                // 1. Ingest tick into native C++20 core (increments volatile standard accumulator)
                _nativeCore.UpdateTick(tick);

                // 2. Fetch high-performance Market Vector from C++ interop memory layout
                var marketVector = _nativeCore.GetMarketVector();
                var marketState = _nativeCore.GetMarketState();

                // 3. Execute ONNX AI model scoring asynchronously
                var evaluation = await _neuralModel.EvaluateAsync(marketVector, ct);

                // 4. Evaluate risk metrics to yield a final trade decision
                var decision = _decisionEngine.Evaluate(evaluation, marketState, currentRiskState);

                // 5. Package state into an Experience Record and queue it instantly for database write (Zero-Block)
                var experience = new ExperienceRecord(
                    tick.Symbol.Name,
                    marketVector.ToFloatArray(),
                    _neuralModel.ModelVersion,
                    evaluation.BuyConfidence,
                    evaluation.SellConfidence,
                    evaluation.RiskScore,
                    evaluation.MarketRegime,
                    decision.Action.ToString()
                );

                _dbWriter.Enqueue(experience);

                // 6. Raise event for UI update loops
                OnAnalysisCompleted?.Invoke(evaluation, decision);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarketIntelligenceCoordinator] Critical error during real-time AI and C++ interop evaluation.");
            }
        }

        public void Dispose()
        {
            // Clean up resources
        }
    }
}