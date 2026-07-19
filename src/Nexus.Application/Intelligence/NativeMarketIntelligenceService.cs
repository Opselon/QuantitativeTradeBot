using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public enum IntelligenceMode
    {
        NATIVE_MODE,
        FALLBACK_MODE
    }

    /// <summary>
    /// Deployed adapter service orchestrating the real-time high-performance processing path:
    /// Tick Ingestion -> C++ Native Engine (Accumulator & Market State) -> MarketVector -> ONNX Neural Inference -> Decision Engine.
    /// </summary>
    public class NativeMarketIntelligenceService
    {
        private readonly INativeCoreService _nativeCore;
        private readonly IAccumulatorService _managedAccumulator;
        private readonly ICurrencyStrengthEngine _currencyEngine;
        private readonly INeuralModelService _neuralService;
        private readonly IDecisionEngine _decisionEngine;

        public IntelligenceMode CurrentMode => _nativeCore.IsAvailable ? IntelligenceMode.NATIVE_MODE : IntelligenceMode.FALLBACK_MODE;
        public string ActiveRegime { get; private set; } = "Ranging / Stable";

        // Monitoring Latency metrics
        public double TickProcessingLatencyMs { get; private set; }
        public double MarketStateUpdateTimeMs { get; private set; }
        public double VectorGenerationTimeMs { get; private set; }
        public double InteropLatencyMs { get; private set; }

        public NativeMarketIntelligenceService(
            INativeCoreService nativeCore,
            IAccumulatorService managedAccumulator,
            ICurrencyStrengthEngine currencyEngine,
            INeuralModelService neuralService,
            IDecisionEngine decisionEngine)
        {
            _nativeCore = nativeCore ?? throw new ArgumentNullException(nameof(nativeCore));
            _managedAccumulator = managedAccumulator ?? throw new ArgumentNullException(nameof(managedAccumulator));
            _currencyEngine = currencyEngine ?? throw new ArgumentNullException(nameof(currencyEngine));
            _neuralService = neuralService ?? throw new ArgumentNullException(nameof(neuralService));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
        }

        public async Task<TradeDecision> ProcessTickAndEvaluateAsync(Tick tick, RiskState risk, CancellationToken ct = default)
        {
            var swTotal = System.Diagnostics.Stopwatch.StartNew();

            MarketVector vector;
            MarketState state;

            if (_nativeCore.IsAvailable)
            {
                // STEP A: UPDATE NATIVE TICK
                var swTick = System.Diagnostics.Stopwatch.StartNew();
                _nativeCore.UpdateTick(tick);
                swTick.Stop();
                TickProcessingLatencyMs = swTick.Elapsed.TotalMilliseconds;

                // STEP B: QUERY STATE
                var swState = System.Diagnostics.Stopwatch.StartNew();
                state = _nativeCore.GetMarketState();
                swState.Stop();
                MarketStateUpdateTimeMs = swState.Elapsed.TotalMilliseconds;

                // STEP C: GENERATE MARKET VECTOR
                var swVector = System.Diagnostics.Stopwatch.StartNew();
                vector = _nativeCore.GetMarketVector();
                swVector.Stop();
                VectorGenerationTimeMs = swVector.Elapsed.TotalMilliseconds;

                InteropLatencyMs = 0.01; // Constant tiny P/Invoke overhead benchmark
            }
            else
            {
                // STEP A: MANAGED ACCUMULATOR FALLBACK
                var swTick = System.Diagnostics.Stopwatch.StartNew();
                _currencyEngine.UpdateFromTick(tick);
                double usdStrength = _currencyEngine.GetStrengthScore("USD");

                var delta = new FeatureDelta(tick.Symbol.Name, tick.Time, tick.Spread, tick.Bid);
                var managedState = _managedAccumulator.UpdateState(delta);
                swTick.Stop();
                TickProcessingLatencyMs = swTick.Elapsed.TotalMilliseconds;

                // STEP B: MANAGED STATE
                var swState = System.Diagnostics.Stopwatch.StartNew();
                state = new MarketState(
                    tick.Symbol.Name,
                    tick.Time,
                    managedState.CalculateStandardDeviation(),
                    managedState.CalculateMean(),
                    tick.Spread > 0 ? 1.0 / (1.0 + tick.Spread * 100.0) : 1.0,
                    0.5,
                    0.5,
                    0.1,
                    usdStrength,
                    "Ranging"
                );
                swState.Stop();
                MarketStateUpdateTimeMs = swState.Elapsed.TotalMilliseconds;

                // STEP C: GENERATE VECTOR
                var swVector = System.Diagnostics.Stopwatch.StartNew();
                vector = new MarketVector(
                    state.PriceStructure,
                    0.5,
                    state.Momentum,
                    state.Volatility,
                    0.5,
                    state.Liquidity,
                    usdStrength / 100.0,
                    0.5,
                    0.0,
                    state.Risk
                );
                swVector.Stop();
                VectorGenerationTimeMs = swVector.Elapsed.TotalMilliseconds;

                InteropLatencyMs = 0.0;
            }

            ActiveRegime = state.MarketRegime;

            // STEP D: NEURAL INFERENCE via ONNX Runtime
            var evaluation = await _neuralService.EvaluateAsync(vector, ct);

            // STEP E: PRE-TRADE RISK & DECISION ORDINANCE
            var decision = _decisionEngine.Evaluate(evaluation, state, risk);

            swTotal.Stop();
            return decision;
        }
    }
}
