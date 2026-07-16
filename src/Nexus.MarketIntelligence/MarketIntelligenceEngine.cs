using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.Core.Enums;
using Nexus.Core.ValueObjects;
using Nexus.MarketIntelligence.Features;
using Nexus.MarketIntelligence.Memory;
using Nexus.MarketIntelligence.MultiTimeframe;
using Nexus.MarketIntelligence.Quality;
using Nexus.MarketIntelligence.Regimes;

namespace Nexus.MarketIntelligence
{
    /// <summary>
    /// The central Market Intelligence and Data Fusion Engine.
    /// Orchestrates data normalization, multi-timeframe synchronization, regime detection, quality scoring, and feature extraction.
    /// Produces a unified <see cref="MarketIntelligenceSnapshot"/>, which is the only source of truth consumed by downstream reasoning components.
    /// </summary>
    public sealed class MarketIntelligenceEngine
    {
        private readonly MultiTimeframeEngine _mtfEngine;
        private readonly MarketRegimeDetector _regimeDetector;
        private readonly MarketQualityEvaluator _qualityEvaluator;
        private readonly FeatureExtractor _featureExtractor;
        private readonly IMarketStateMemory? _stateMemory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketIntelligenceEngine"/> class.
        /// </summary>
        public MarketIntelligenceEngine(
            MultiTimeframeEngine mtfEngine,
            MarketRegimeDetector regimeDetector,
            MarketQualityEvaluator qualityEvaluator,
            FeatureExtractor featureExtractor,
            IMarketStateMemory? stateMemory = null)
        {
            _mtfEngine = mtfEngine ?? throw new ArgumentNullException(nameof(mtfEngine));
            _regimeDetector = regimeDetector ?? throw new ArgumentNullException(nameof(regimeDetector));
            _qualityEvaluator = qualityEvaluator ?? throw new ArgumentNullException(nameof(qualityEvaluator));
            _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
            _stateMemory = stateMemory;
        }

        /// <summary>
        /// Processes raw, heterogeneous market data to generate a single normalized and explainable <see cref="MarketIntelligenceSnapshot"/>.
        /// </summary>
        /// <param name="symbol">The asset symbol.</param>
        /// <param name="timeframeData">Heterogeneous multi-timeframe candle histories (ordered chronologically).</param>
        /// <param name="latestTick">The latest bid/ask tick pricing information.</param>
        /// <param name="activeSession">The active trading session context.</param>
        /// <param name="currentSpreadPoints">The current trading spread in points.</param>
        /// <param name="averageSpreadPoints">The historical average trading spread in points.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A unified, deterministic <see cref="MarketIntelligenceSnapshot"/>.</returns>
        public async Task<MarketIntelligenceSnapshot> ProcessMarketDataAsync(
            string symbol,
            IReadOnlyDictionary<TimeframeType, IReadOnlyList<Candle>> timeframeData,
            Tick latestTick,
            MarketSession activeSession,
            double currentSpreadPoints,
            double averageSpreadPoints,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));

            DateTime timestampUtc = DateTime.UtcNow;

            #region 1. Multi-Timeframe Alignment Evaluation

            MultiTimeframeState mtfState = _mtfEngine.Synchronize(symbol, timeframeData);

            #endregion

            #region 2. Regime Classification

            // Use the finest available resolution (M1) candles for tactical regime detection
            if (!timeframeData.TryGetValue(TimeframeType.M1, out var tacticalCandles))
            {
                // Fallback to any available timeframe candles if M1 is missing
                tacticalCandles = new List<Candle>();
                foreach (var list in timeframeData.Values)
                {
                    if (list.Count > tacticalCandles.Count)
                    {
                        tacticalCandles = list;
                    }
                }
            }

            IReadOnlyDictionary<string, RegimeClassification> regimes = _regimeDetector.DetectRegimes(tacticalCandles, currentSpreadPoints);
            RegimeClassification dominantRegime = _regimeDetector.GetDominantRegime(regimes);

            #endregion

            #region 3. Market Quality & Risk Evaluation

            MarketQualityScore qualityScore = _qualityEvaluator.EvaluateQuality(tacticalCandles, currentSpreadPoints, averageSpreadPoints);

            #endregion

            #region 4. Feature Vector Extraction

            ExtractedFeatures features = _featureExtractor.Extract(symbol, tacticalCandles, latestTick, activeSession, mtfState);

            #endregion

            #region 5. Unified Domain MarketState Synthesis

            // Map the mathematical models to the Core.Entities.MarketState values
            // volatility -> VolatilityStability rating divided by 100 to map to [0, 1] percentage
            double volatilityPct = Math.Clamp((100.0 - qualityScore.VolatilityStability) / 100.0, 0.0, 1.0);

            // momentum -> Consensus score mapped from 0-100 to -1.0 to 1.0
            double momentumScaled = (mtfState.ConsensusScore - 50.0) / 50.0;

            // liquidity -> Liquidity rating divided by 100 to map to [0, 1]
            double liquidityScaled = qualityScore.Liquidity / 100.0;

            // priceStructure -> TrendQuality rating mapped to [0, 1]
            double structureScaled = qualityScore.TrendQuality / 100.0;

            // probability -> Dominant regime confidence mapped to [0, 1]
            double probabilityPct = dominantRegime.Confidence / 100.0;

            // risk -> Execution risk mapped to [0, 1]
            double riskScaled = qualityScore.ExecutionRisk / 100.0;

            // currencyStrength -> default or calculated from features
            double currencyStrengthVal = 50.0; // Baseline

            var coreMarketState = new MarketState(
                symbol,
                timestampUtc,
                volatilityPct,
                momentumScaled,
                liquidityScaled,
                structureScaled,
                probabilityPct,
                riskScaled,
                currencyStrengthVal,
                dominantRegime.Regime);

            #endregion

            #region 6. Optional Historical Pattern Registry Store

            if (_stateMemory != null)
            {
                await _stateMemory.StoreStateAsync(coreMarketState, features, "Analyzed", ct);
            }

            #endregion

            return new MarketIntelligenceSnapshot(
                symbol,
                timestampUtc,
                coreMarketState,
                mtfState,
                dominantRegime,
                regimes,
                qualityScore,
                features);
        }
    }
}
