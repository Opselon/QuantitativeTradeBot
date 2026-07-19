// Imports essential core system operations for mathematics and dynamic properties. [Ref: Core-Libraries]
using System;
// Imports thread-safe concurrent collections to record sweep events under high thread pool concurrency. [Ref: Concurrency-Control]
using System.Collections.Concurrent;
// Imports generic collection types representing sequences and lookups. [Ref: Collections-Generic]
using System.Collections.Generic;
// Imports compiler optimization services to bind state tables to context lifespans. [Ref: Native-Optimizations]
using System.Runtime.CompilerServices;
// Imports cancellation token systems to safely manage asynchronous lifecycle aborts. [Ref: Thread-Abort]
using System.Threading;
// Imports async task wrappers for parallel high-frequency compute threads. [Ref: Async-Execution]
using System.Threading.Tasks;
// Imports project price action orchestrator contracts to guarantee structural compatibility. [Ref: Project-Contracts]
using Nexus.PriceAction.Abstractions;
// Maps the core domain Candle model under an alias to eliminate type resolution ambiguities. [Ref: Domain-Mapping]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the high-performance structural price action analysis namespace. [Ref: SMC-Pro]
namespace Nexus.PriceAction.Structural
{
    // Declares the directional type of the identified stop-hunt sweep. [Ref: Sweep-Type-Enum]
    public enum LiquiditySweepType
    {
        // Bullish Sweep: Price swept a Swing Low level, absorbing buy stops to reverse upward. [Ref: Sweep-Bullish]
        Bullish = 1,
        // Bearish Sweep: Price swept a Swing High level, absorbing sell stops to reverse downward. [Ref: Sweep-Bearish]
        Bearish = 2
    }

    // Encapsulates the immutable quantitative and mechanical metrics of a verified Stop Hunt event. [Ref: Sweep-State-Record]
    public record LiquiditySweep(
        // The precise timestamp of the sweep candle showing long-tail rejection. [Ref: Sweep-Timestamp]
        DateTime Timestamp,
        // The exact price coordinate of the swept Swing High/Low level. [Ref: Swept-Price-Level]
        decimal SweptPriceLevel,
        // The historical timestamp when the swept Swing level was originally formed. [Ref: Swept-Level-Timestamp]
        DateTime SweptLevelTimestamp,
        // The maximum distance the wick penetrated beyond the key structural pivot level. [Ref: Penetration-Depth]
        decimal PenetrationDepth,
        // The size of the rejection shadow relative to the total trading range of the sweep candle. [Ref: Rejection-Wick-Ratio]
        decimal RejectionWickRatio,
        // The ratio of the sweep candle volume to its 20-period simple moving average. [Ref: Volume-Absorption-Ratio]
        decimal VolumeAbsorptionRatio,
        // Specifies if the stop hunt represents a Bullish or Bearish reversal setup. [Ref: Sweep-Category]
        LiquiditySweepType Type,
        // The sequential index of the sweep candle in the current price feed. [Ref: Sweep-Chrono-Index]
        int CandleIndex
    );

    // Advanced non-blocking engine to detect stop-hunts and calculate institutional absorption rates. [Ref: SRP-LiquiditySweep]
    public class LiquiditySweepDetector : IPriceActionEngine
    {
        // Thread-safely maps pipeline contexts to their respective liquidity sweep registries. [Ref: Sweep-Cache-Registry]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, LiquiditySweep>> SweepProCache = new();

        // Volume lookback period to calculate average market liquidity. [Ref: Volume-SMA-Period]
        private const int VolumeMaPeriod = 20;

        // Minimum rejection wick ratio required to confirm institutional price absorption (50% of range). [Ref: Wick-Filter-Ratio]
        private const decimal MinRejectionWickRatio = 0.50m;

        // Public query API to retrieve a computed sweep event from context memory safely. [Ref: Sweep-Public-Query]
        public static LiquiditySweep? GetLiquiditySweep(PriceActionContext context, DateTime timestamp)
        {
            // Tries to locate the context instance within the thread-safe dynamic weak reference table. [Ref: WeakTable-Query-Sweeps]
            if (SweepProCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed Liquidity Sweep record if registered under the given timestamp key. [Ref: Retrieve-Sweep-Record]
                if (dictionary.TryGetValue(timestamp, out var ls))
                {
                    // Returns the successfully retrieved stop-hunt object. [Ref: Return-Sweep-Value]
                    return ls;
                }
            }
            // Returns null if the specified timestamp has no valid stop hunt registered. [Ref: Fallback-Sweep-Null]
            return null;
        }

        // Public API to extract all registered Liquidity Sweeps sorted chronologically. [Ref: Sweep-Bulk-Extraction]
        public static IReadOnlyList<LiquiditySweep> GetAllLiquiditySweeps(PriceActionContext context)
        {
            // Locates the specific context store inside the optimized caching memory table. [Ref: Partition-Extract-Sweeps]
            if (SweepProCache.TryGetValue(context, out var dictionary))
            {
                // Takes an atomic snapshot list of all computed Liquidity Sweep records. [Ref: Snapshot-Copy-Sweeps]
                var list = new List<LiquiditySweep>(dictionary.Values);
                // Sorts the list chronologically according to the sweep candle index sequence. [Ref: Sequence-Sorting-Sweeps]
                list.Sort((x, y) => x.CandleIndex.CompareTo(y.CandleIndex));
                // Exposes the sorted sequence as a clean read-only array to prevent external mutations. [Ref: Safe-Expose-Sweeps]
                return list;
            }
            // Returns an empty list to prevent downstream processing errors. [Ref: Null-Safe-Exit-Sweeps]
            return Array.Empty<LiquiditySweep>();
        }

        // Asynchronously processes the price buffer to locate wicks that hunted confirmed swing highs or lows. [Ref: Main-Pipeline-Sweeps]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Guards against null contexts to protect system boundaries from unhandled errors. [Ref: Input-Guard-Sweeps]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Thread-safely obtains or instantiates the liquidity sweep storage collection for this context. [Ref: Store-Acquisition-Sweeps]
            var sweepDict = SweepProCache.GetOrCreateValue(context);

            // Obtains all validated swing points computed in the first engine phase. [Ref: Get-Swing-Data-Sweeps]
            var swings = SwingPointDetector.GetAllSwingPoints(context);

            // Obtains the physical size of the raw price feed buffer. [Ref: Feed-Extraction-Sweeps]
            int candleCount = context.RawCandles.Count;

            // Halts execution if the historical feed lacks the minimum data threshold for analysis. [Ref: Minimum-Feed-Check-Sweeps]
            if (candleCount < 5 || swings.Count == 0)
            {
                // Safely passes back the untouched context through the async pipeline wrapper. [Ref: Early-Pass-Sweeps]
                return Task.FromResult(context);
            }

            // Traverses chronological candles starting from the first potential swing verification point. [Ref: Chrono-Traversal-Sweeps]
            for (int i = 1; i < candleCount; i++)
            {
                // Enables dynamic thread context interrupting to manage high system utilization. [Ref: Thread-Check-Sweeps]
                cancellationToken.ThrowIfCancellationRequested();

                // References the current potential sweep candle. [Ref: Sweep-Test-Candle]
                DomainCandle currentCandle = context.RawCandles[i];
                decimal currentHigh = (decimal)currentCandle.High.Value;
                decimal currentLow = (decimal)currentCandle.Low.Value;
                decimal currentOpen = (decimal)currentCandle.Open.Value;
                decimal currentClose = (decimal)currentCandle.Close.Value;

                // Calculates total trading range of the testing candle. [Ref: Total-Range-Math]
                decimal totalRange = currentHigh - currentLow;

                // Skips processing for frozen or abnormal candles to avoid division-by-zero errors. [Ref: Range-Zero-Guard]
                if (totalRange == 0) continue;

                // Identifies all active swing points formed prior to the current index to avoid lookahead bias. [Ref: Active-Swings-Lookup]
                foreach (var swing in swings)
                {
                    // Restricts search to confirmed swing points that occurred historically before this candle. [Ref: History-Guard]
                    if (swing.Index >= i || !swing.IsConfirmed) continue;

                    // Initializes sweep evaluation parameters. [Ref: Sweep-Params-Init]
                    bool isBullishSweep = false;
                    bool isBearishSweep = false;
                    decimal penetration = 0m;
                    decimal rejectionWick = 0m;

                    // Evaluates Bullish Liquidity Sweep (Stop Hunt of a Swing Low). [Ref: Bullish-Sweep-Condition]
                    if (!swing.IsHigh)
                    {
                        // Wick penetrates below the swing low, but body (open/close) successfully closes above the low. [Ref: Bullish-Wick-Penetration]
                        if (currentLow < swing.Price && currentOpen > swing.Price && currentClose > swing.Price)
                        {
                            // Flags a valid bullish stop hunt. [Ref: Mark-Bullish-Sweep]
                            isBullishSweep = true;
                            // Calculates exact depth of the stop hunt penetration below the support floor. [Ref: Penetration-Math-Bullish]
                            penetration = swing.Price - currentLow;
                            // Rejection wick is the size of the lower shadow (bottom of body down to low). [Ref: Lower-Shadow-Math]
                            decimal bodyMin = Math.Min(currentOpen, currentClose);
                            rejectionWick = bodyMin - currentLow;
                        }
                    }
                    // Evaluates Bearish Liquidity Sweep (Stop Hunt of a Swing High). [Ref: Bearish-Sweep-Condition]
                    else if (swing.IsHigh)
                    {
                        // Wick penetrates above the swing high, but body (open/close) successfully closes below the high. [Ref: Bearish-Wick-Penetration]
                        if (currentHigh > swing.Price && currentOpen < swing.Price && currentClose < swing.Price)
                        {
                            // Flags a valid bearish stop hunt. [Ref: Mark-Bearish-Sweep]
                            isBearishSweep = true;
                            // Calculates exact depth of the stop hunt penetration above the resistance ceiling. [Ref: Penetration-Math-Bearish]
                            penetration = currentHigh - swing.Price;
                            // Rejection wick is the size of the upper shadow (high down to top of body). [Ref: Upper-Shadow-Math]
                            decimal bodyMax = Math.Max(currentOpen, currentClose);
                            rejectionWick = currentHigh - bodyMax;
                        }
                    }

                    // Proceeds with deep validation if a directional stop hunt is confirmed. [Ref: Validate-Rejection-Force]
                    if (isBullishSweep || isBearishSweep)
                    {
                        // Calculates the ratio of the rejection wick relative to the total candle range. [Ref: Rejection-Ratio-Math]
                        decimal wickRatio = rejectionWick / totalRange;

                        // Filters out weak, insignificant wicks to avoid trading market noise. [Ref: Rejection-Ratio-Filter]
                        if (wickRatio < MinRejectionWickRatio) continue;

                        // Calculates average historical trading volume to measure institutional absorption rates. [Ref: Volume-SMA]
                        decimal avgVolume = CalculateVolumeSma(context, i);

                        // Calculates the volume absorption ratio (current volume relative to average). [Ref: Absorption-Ratio-Math]
                        decimal volumeRatio = avgVolume > 0 ? (decimal)currentCandle.Volume.Value / avgVolume : 1.0m;

                        // Compiles validated Stop Hunt parameters into an immutable record object. [Ref: Compile-Sweep]
                        var sweep = new LiquiditySweep(
                            Timestamp: currentCandle.Timestamp,
                            SweptPriceLevel: swing.Price,
                            SweptLevelTimestamp: swing.Timestamp,
                            PenetrationDepth: penetration,
                            RejectionWickRatio: wickRatio,
                            VolumeAbsorptionRatio: volumeRatio,
                            Type: isBullishSweep ? LiquiditySweepType.Bullish : LiquiditySweepType.Bearish,
                            CandleIndex: i
                        );

                        // Commits the Liquidity Sweep record to the context storage safely. [Ref: Commit-Store-Sweeps]
                        sweepDict.AddOrUpdate(currentCandle.Timestamp, sweep, (key, existing) => sweep);

                        // Breaks out of inner swing loop to prevent registering multiple sweep events on the same candle. [Ref: Sweep-Loop-Break]
                        break;
                    }
                }
            }

            // Passes the context up the pipeline with completed Liquidity Sweeps. [Ref: Task-Complete-Sweeps]
            return Task.FromResult(context);
        }

        // Calculates simple moving average of volume dynamically. [Ref: Volume-SMA-Helper]
        private decimal CalculateVolumeSma(PriceActionContext context, int currentIndex)
        {
            // Resolves dynamic start bounds to prevent index-out-of-bounds exceptions. [Ref: Vol-Safe-Start]
            int startIndex = Math.Max(0, currentIndex - VolumeMaPeriod);
            int count = currentIndex - startIndex;

            // Guard against division-by-zero during early initialization frames. [Ref: Vol-Count-Guard]
            if (count == 0) return 0m;

            // Accumulates historical volume values. [Ref: Vol-Summation-Loop]
            decimal volSum = 0m;
            for (int k = startIndex; k < currentIndex; k++)
            {
                // Accumulates raw volume converted to decimal. [Ref: Vol-Accumulate]
                volSum += (decimal)context.RawCandles[k].Volume.Value;
            }

            // Returns average volume score representing benchmark liquidity. [Ref: Return-Vol-SMA]
            return volSum / count;
        }
    }
}