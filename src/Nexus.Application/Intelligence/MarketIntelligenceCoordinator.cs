using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public class MarketIntelligenceCoordinator : IDisposable
    {
        private readonly INativeCoreService _nativeCore;
        private readonly INeuralModelService _neuralModel;
        private readonly IDecisionEngine _decisionEngine;
        private readonly IExperienceDatabaseWriter _dbWriter;
        private readonly IMt5BridgeService _bridgeService;
        private readonly ILogger<MarketIntelligenceCoordinator> _logger;
        private readonly CancellationTokenSource _cts = new();

        public event Action<EvaluationResult, TradeDecision>? OnAnalysisCompleted;

        public MarketIntelligenceCoordinator(
            INativeCoreService nativeCore,
            INeuralModelService neuralModel,
            IDecisionEngine decisionEngine,
            IExperienceDatabaseWriter dbWriter,
            IMt5BridgeService bridgeService,
            ILogger<MarketIntelligenceCoordinator> logger)
        {
            _nativeCore = nativeCore ?? throw new ArgumentNullException(nameof(nativeCore));
            _neuralModel = neuralModel ?? throw new ArgumentNullException(nameof(neuralModel));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _dbWriter = dbWriter ?? throw new ArgumentNullException(nameof(dbWriter));
            _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _bridgeService.OnTickReceived += OnLiveTickReceived;
        }

        private void OnLiveTickReceived(PriceTickEnvelope envelope)
        {
            if (envelope == null) return;

            Task.Run(async () =>
            {
                try
                {
                    var symbol = new Core.ValueObjects.Symbol(envelope.SymbolName);
                    var tick = new Tick(symbol, envelope.Timestamp, envelope.Bid, envelope.Ask);

                    var currentRisk = EvaluateCurrentRiskState();
                    await ProcessTickAsync(tick, currentRisk, _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MarketIntelligenceCoordinator] Error running tick interop pipeline.");
                }
            });
        }

        private RiskState EvaluateCurrentRiskState()
        {
            return new RiskState(500.0, 10.0, 1.2, 0, 0.10, false);
        }

        public async Task ProcessTickAsync(Tick tick, RiskState currentRiskState, CancellationToken ct)
        {
            try
            {
                _nativeCore.UpdateTick(tick);

                var marketVector = _nativeCore.GetMarketVector();
                var marketState = _nativeCore.GetMarketState();

                var evaluation = await _neuralModel.EvaluateAsync(marketVector, ct);
                var decision = _decisionEngine.Evaluate(evaluation, marketState, currentRiskState);

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

                OnAnalysisCompleted?.Invoke(evaluation, decision);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarketIntelligenceCoordinator] Critical error during real-time AI and C++ interop evaluation.");
            }
        }

        public void Dispose()
        {
            _bridgeService.OnTickReceived -= OnLiveTickReceived;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}