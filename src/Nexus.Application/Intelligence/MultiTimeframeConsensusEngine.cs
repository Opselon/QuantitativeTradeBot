using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using System.Collections.Concurrent;

namespace Nexus.Application.Intelligence
{
    public sealed class MultiTimeframeConsensusEngine : IMultiTimeframeConsensusEngine
    {
        private static readonly TimeSpan MaxSignalAge = TimeSpan.FromHours(1);
        private readonly ConcurrentDictionary<TimeframeInterval, MultiTimeframeSignal> _signals = new();

        public void RegisterTimeframeSignal(MultiTimeframeSignal signal)
        {
            if (signal == null)
            {
                throw new ArgumentNullException(nameof(signal));
            }
            _signals[signal.Timeframe] = signal;
        }

        public ConsensusState GetCurrentConsensus()
        {
            DateTime now = DateTime.UtcNow;

            // Filter out stale signals older than MaxSignalAge (1 hour)
            var activeSignals = _signals.Values
                .Where(s => now - s.TimestampUtc <= MaxSignalAge)
                .ToList();

            if (activeSignals.Count == 0)
            {
                return new ConsensusState(
                    TrendDirection.NEUTRAL,
                    0.0,
                    entryTriggered: false,
                    overallConfidence: 0.0,
                    "No fresh active timeframe signals registered (or all signals have expired).",
                    activeSignals,
                    now
                );
            }

            // Group timeframes according to Stockfish Multi-Timeframe Philosophy:
            // 1. Bias/Direction/Regime: D1, H4
            var biasSignals = activeSignals
                .Where(s => s.Timeframe == TimeframeInterval.D1 || s.Timeframe == TimeframeInterval.H4)
                .ToList();

            // 2. Confirmation: H1, M30
            var confirmSignals = activeSignals
                .Where(s => s.Timeframe == TimeframeInterval.H1 || s.Timeframe == TimeframeInterval.M30)
                .ToList();

            // 3. Tactical Entry: M15, M5, M1
            var entrySignals = activeSignals
                .Where(s => s.Timeframe == TimeframeInterval.M15 || s.Timeframe == TimeframeInterval.M5 || s.Timeframe == TimeframeInterval.M1)
                .ToList();

            TrendDirection dominantBias = TrendDirection.NEUTRAL;
            double biasStrength = 0.0;
            double overallConfidence = 0.0;

            // Establish Dominant Direction/Bias (from D1/H4)
            if (biasSignals.Count > 0)
            {
                int bullishCount = biasSignals.Count(s => s.Trend == TrendDirection.BULLISH);
                int bearishCount = biasSignals.Count(s => s.Trend == TrendDirection.BEARISH);

                if (bullishCount > bearishCount)
                {
                    dominantBias = TrendDirection.BULLISH;
                    biasStrength = biasSignals.Where(s => s.Trend == TrendDirection.BULLISH).Average(s => s.Strength);
                }
                else if (bearishCount > bullishCount)
                {
                    dominantBias = TrendDirection.BEARISH;
                    biasStrength = biasSignals.Where(s => s.Trend == TrendDirection.BEARISH).Average(s => s.Strength);
                }
                overallConfidence = biasSignals.Average(s => s.Confidence);
            }

            // Verify Confirmation (from H1/M30)
            bool isConfirmed = false;
            if (dominantBias != TrendDirection.NEUTRAL && confirmSignals.Count > 0)
            {
                // Confirmation matches dominant bias if at least one confirming timeframe aligns with dominant bias
                var matchingConfirms = confirmSignals.Where(s => s.Trend == dominantBias).ToList();
                if (matchingConfirms.Count > 0)
                {
                    isConfirmed = true;
                    // Blend confirmation confidence into overall confidence
                    overallConfidence = (overallConfidence + matchingConfirms.Average(s => s.Confidence)) / 2.0;
                }
            }
            else if (confirmSignals.Count == 0)
            {
                // If no confirmation timeframes are registered, we fall back to biased direction directly
                isConfirmed = true;
            }

            // Trigger Entry (from M15/M5/M1 tactical alignment)
            bool entryTriggered = false;
            if (dominantBias != TrendDirection.NEUTRAL && isConfirmed && entrySignals.Count > 0)
            {
                var matchingEntrySignals = entrySignals.Where(s => s.Trend == dominantBias).ToList();
                // If majority of entry signals match the bias
                if (matchingEntrySignals.Count >= (entrySignals.Count + 1) / 2)
                {
                    double avgEntryConfidence = matchingEntrySignals.Average(s => s.Confidence);
                    if (avgEntryConfidence >= 0.60)
                    {
                        entryTriggered = true;
                    }
                }
            }

            string summary = $"Dominant Bias: {dominantBias} (Strength: {biasStrength:P0}, Conf: {overallConfidence:P0}). " +
                             $"Active: {activeSignals.Count} (Bias: {biasSignals.Count}, Confirm: {confirmSignals.Count}, Entry: {entrySignals.Count}). " +
                             $"Confirmed: {(isConfirmed ? "YES" : "NO")}. " +
                             $"Tactical Entry: {(entryTriggered ? "TRIGGERED" : "WAIT")}.";

            return new ConsensusState(
                dominantBias,
                biasStrength,
                entryTriggered,
                overallConfidence,
                summary,
                activeSignals,
                now
            );
        }
    }
}
