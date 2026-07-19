// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   DOMAIN (Core Entity)
// FILE:    MarketKnowledgeRow.cs
// DESCRIPTION: Institutional-grade structure for market knowledge representation.
// ============================================================================

using System;
using System.Runtime.InteropServices;

namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Represents the high-performance, memory-aligned structure of a single market knowledge sample.
    /// This structure contains 34 engineered features derived from the Price Action and ICT/SMC engines.
    /// Used by both the Training Pipeline and the Real-time Inference Engine.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MarketKnowledgeRow
    {
        // 1. Temporal Metadata
        public long TimestampUnixNano;

        // 2. Price Action Basic & Geometry Features
        public float Open;
        public float High;
        public float Low;
        public float Close;
        public float Volume;
        public float Spread;

        // 3. Candle Body & Wick Structure
        public float BodySize;
        public float BodyRatio;
        public float UpperWickRatio;
        public float LowerWickRatio;
        public float DojiScore;

        // 4. Momentum & Volatility
        public float AtrProxy14;
        public float RangeMean20;
        public float VolumeMean20;
        public float VolumeRatio;

        // 5. Market Structure & Swings
        public float BreakHigh20;
        public float BreakLow20;
        public float HigherHigh;
        public float LowerLow;

        // 6. Regime & Context
        public float BullishTrend;
        public float BearishTrend;
        public float SessionId; // e.g., 0: London, 1: NY, etc.

        // 7. Advanced ICT/SMC Liquidity Features
        public float FairValueGapBullish;
        public float FairValueGapBearish;
        public float MarketStructureShift;
        public float OrderBlockProximity;
        public float LiquiditySweepScore;

        // 8. Statistical Metadata
        public float ReliabilityScore;
        public float RegimeStability;
        public float FeatureQuality;

        // 9. Targets (Labels)
        public float FutureReturn;
        public float ExpectedRisk;
        public float ExpectedDrawdown;
    }
}