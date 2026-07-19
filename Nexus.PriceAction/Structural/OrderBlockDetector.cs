// Imports base system types and mathematical functions. [Ref: Core-Libraries]
using System;
// Imports thread-safe concurrent collections to avoid race conditions on high-frequency live ticks. [Ref: Concurrency-Control]
using System.Collections.Concurrent;
// Imports generic collections representing chronological arrays and list buffers. [Ref: Collections-Generic]
using System.Collections.Generic;
// Imports compiler runtime services to bind dynamic properties to context instances safely. [Ref: Native-Optimizations]
using System.Runtime.CompilerServices;
// Imports cancellation thread controls to enforce safe task terminations. [Ref: Thread-Abort]
using System.Threading;
// Imports task parallelism abstractions for async calculations. [Ref: Async-Execution]
using System.Threading.Tasks;
// Imports pipeline context and engine interface abstractions. [Ref: Project-Contracts]
using Nexus.PriceAction.Abstractions;
// Maps the Core Domain Candle entity to prevent naming conflicts with local folders. [Ref: Domain-Mapping]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the high-performance structural price action analysis namespace. [Ref: SMC-Pro]
namespace Nexus.PriceAction.Structural
{
    // Declares the classification category of the identified institutional block. [Ref: OB-Directional-Type]
    public enum OrderBlockType
    {
        // Bullish block zone representing heavy institutional buy orders waiting for mitigation. [Ref: OB-Bullish]
        Bullish = 1,
        // Bearish block zone representing heavy institutional sell orders waiting for mitigation. [Ref: OB-Bearish]
        Bearish = 2
    }

    // Encapsulates the immutable structural properties and validation states of an active Order Block. [Ref: OB-State-Record]
    public record OrderBlock(
        // The unique timestamp of the source order block candle. [Ref: OB-Timestamp]
        DateTime Timestamp,
        // The upper horizontal boundary of the order block zone (High of the candle). [Ref: OB-High-Boundary]
        decimal HighBoundary,
        // The lower horizontal boundary of the order block zone (Low of the candle). [Ref: OB-Low-Boundary]
        decimal LowBoundary,
        // The exact open price level of the order block candle. [Ref: OB-Open-Price]
        decimal OpenPrice,
        // The exact close price level of the order block candle. [Ref: OB-Close-Price]
        decimal ClosePrice,
        // The directional category (Bullish/Bearish) determining strategy bias. [Ref: OB-Type]
        OrderBlockType Type,
        // Flags if a subsequent candle has tested (touched) the order block zone. [Ref: OB-Test-Status]
        bool IsTested,
        // Flags if the market has invalidated the block by closing beyond its invalidation boundary. [Ref: OB-Invalidation-Status]
        bool IsInvalidated,
        // The exact timestamp of the first price touch (test) of this block. [Ref: OB-Test-Timestamp]
        DateTime? TestTimestamp,
        // Composite quality score calculated using breakout momentum and volume parameters. [Ref: OB-Score-Metric]
        decimal SignificanceScore,
        // The sequential index of the source candle in the data stream. [Ref: OB-Chrono-Index]
        int CandleIndex
    );

    // Advanced non-blocking engine to dynamically detect, score, and track institutional Order Blocks. [Ref: SRP-OB]
    public class OrderBlockDetector : IPriceActionEngine
    {
        // WeakTable instance to cache detected order blocks bound to live context life cycles. [Ref: OB-Cache-Registry]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, OrderBlock>> ObProCache = new();

        // Public query API to retrieve a computed order block from context memory safely. [Ref: OB-Public-Query]
        public static OrderBlock? GetOrderBlock(PriceActionContext context, DateTime timestamp)
        {
            // Tries to locate the context instance within the thread-safe dynamic weak reference table. [Ref: WeakTable-Query-OB]
            if (ObProCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed Order Block record if registered under the given timestamp key. [Ref: Retrieve-OB-Record]
                if (dictionary.TryGetValue(timestamp, out var ob))
                {
                    // Returns the successfully retrieved order block object. [Ref: Return-OB-Value]
                    return ob;
                }
            }
            // Returns null if the specified timestamp has no valid order block registered. [Ref: Fallback-OB-Null]
            return null;
        }

        // Public API to extract all registered Order Blocks sorted chronologically. [Ref: OB-Bulk-Extraction]
        public static IReadOnlyList<OrderBlock> GetAllOrderBlocks(PriceActionContext context)
        {
            // Locates the specific context store inside the optimized caching memory table. [Ref: Partition-Extract-OB]
            if (ObProCache.TryGetValue(context, out var dictionary))
            {
                // Takes an atomic snapshot list of all computed Order Block records. [Ref: Snapshot-Copy-OB]
                var list = new List<OrderBlock>(dictionary.Values);
                // Sorts the list chronologically according to the center index sequence. [Ref: Sequence-Sorting-OB]
                list.Sort((x, y) => x.CandleIndex.CompareTo(y.CandleIndex));
                // Exposes the sorted sequence as a clean read-only array to prevent external mutations. [Ref: Safe-Expose-OB]
                return list;
            }
            // Returns an empty list to prevent downstream processing errors. [Ref: Null-Safe-Exit-OB]
            return Array.Empty<OrderBlock>();
        }

        // Asynchronously detects, scores, and tracks Order Blocks over candle buffers. [Ref: Main-Pipeline-OB]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Guards against null contexts to protect system boundaries from unhandled errors. [Ref: Input-Guard-OB]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Thread-safely obtains or instantiates the order block storage collection for this context. [Ref: Store-Acquisition-OB]
            var obDict = ObProCache.GetOrCreateValue(context);

            // Measures the current size of the historical candle feed buffer. [Ref: Buffer-Size-OB]
            int candleCount = context.RawCandles.Count;

            // Demands a minimum of three candles to reconstruct order block setups. [Ref: Sequence-Guard-OB]
            if (candleCount < 3)
            {
                // Exits immediately if data is insufficient for standard evaluations. [Ref: Early-Pass-OB]
                return Task.FromResult(context);
            }

            // Pulls calculated Fair Value Gaps from context database to confirm institutional impulse. [Ref: Retrieve-FVG-Data]
            var fvgs = FairValueGapDetector.GetAllFvgs(context);

            // Creates a fast lookup set of FVG center indices for O(1) correlation matching. [Ref: FVG-Index-Map]
            var fvgIndexSet = new HashSet<int>();
            // Populates index lookup map. [Ref: Map-Loop]
            foreach (var fvg in fvgs)
            {
                // Registers center index. [Ref: Map-Add]
                fvgIndexSet.Add(fvg.CenterIndex);
            }

            // Loops through chronological candles starting from index 0 up to count - 2. [Ref: Chrono-Traversal-OB]
            for (int i = 0; i < candleCount - 1; i++)
            {
                // Enables dynamic thread context interrupting to manage high system utilization. [Ref: Thread-Check-OB]
                cancellationToken.ThrowIfCancellationRequested();

                // Order block candle is the candle prior to the impulsive move index. [Ref: OB-Candle-Ref]
                DomainCandle obCandle = context.RawCandles[i];
                // Impulsive breakout candle index is subsequent. [Ref: Breakout-Index-Ref]
                int breakoutIndex = i + 1;

                // Verifies if the subsequent candle generated a validated Fair Value Gap. [Ref: FVG-Verification]
                if (!fvgIndexSet.Contains(breakoutIndex))
                {
                    // Skips because institutional impulse is not confirmed by a liquidity void (FVG). [Ref: Skip-No-Imbalance]
                    continue;
                }

                // References the explosive breakout candle. [Ref: Breakout-Candle-Ref]
                DomainCandle breakoutCandle = context.RawCandles[breakoutIndex];

                // Converts boundaries of the Order Block candle. [Ref: OB-Bounds-Cast]
                decimal obHigh = (decimal)obCandle.High.Value;
                decimal obLow = (decimal)obCandle.Low.Value;
                decimal obOpen = (decimal)obCandle.Open.Value;
                decimal obClose = (decimal)obCandle.Close.Value;

                // Converts boundaries of the impulsive breakout candle. [Ref: Breakout-Bounds-Cast]
                decimal boOpen = (decimal)breakoutCandle.Open.Value;
                decimal boClose = (decimal)breakoutCandle.Close.Value;

                // Checks direction of the explosive breakout candle. [Ref: Breakout-Direction-Check]
                bool isBullishBreakout = boClose > boOpen;
                bool isBearishBreakout = boOpen > boClose;

                // Evaluates if the candidate matches Bullish Order Block criteria. [Ref: Bullish-OB-Criteria]
                bool isBullishOb = isBullishBreakout && obClose < obOpen; // Down-close candle before up-move
                // Evaluates if the candidate matches Bearish Order Block criteria. [Ref: Bearish-OB-Criteria]
                bool isBearishOb = isBearishBreakout && obClose > obOpen; // Up-close candle before down-move

                // Proceeds only if a valid directional Order Block is confirmed in the sequence. [Ref: OB-Check]
                if (isBullishOb || isBearishOb)
                {
                    // Instantiates test and invalidation tracking states before scanning future price actions. [Ref: OB-Mitigation-Prep]
                    bool isTested = false;
                    bool isInvalidated = false;
                    DateTime? testTime = null;

                    // Scans forward from the index after the breakout candle to track testing and invalidation. [Ref: OB-Forward-Scan]
                    for (int j = breakoutIndex + 1; j < candleCount; j++)
                    {
                        // References the subsequent candle testing the order block zone. [Ref: OB-Test-Candle]
                        DomainCandle futureCandle = context.RawCandles[j];
                        decimal futureHigh = (decimal)futureCandle.High.Value;
                        decimal futureLow = (decimal)futureCandle.Low.Value;
                        decimal futureClose = (decimal)futureCandle.Close.Value;

                        // Evaluates mitigation logic for Bullish Order Blocks. [Ref: Bullish-OB-Mitigation-Logic]
                        if (isBullishOb)
                        {
                            // Checks if a subsequent candle entered or touched the upper boundary of the OB. [Ref: Bullish-OB-Touch]
                            if (futureLow <= obHigh && !isTested)
                            {
                                // Flags initial test as active. [Ref: OB-Mark-Tested]
                                isTested = true;
                                // Records exact test timestamp. [Ref: OB-Log-Test-Timestamp]
                                testTime = futureCandle.Timestamp;
                            }

                            // Checks if the block has been invalidated (price closed below the invalidation boundary/low of OB). [Ref: Bullish-OB-Invalidation]
                            if (futureClose < obLow)
                            {
                                // Flags block as invalidated. [Ref: OB-Mark-Invalidated]
                                isInvalidated = true;
                                // Breaks out of forward scan since the block is dead. [Ref: OB-Scan-Break]
                                break;
                            }
                        }
                        // Evaluates mitigation logic for Bearish Order Blocks. [Ref: Bearish-OB-Mitigation-Logic]
                        else if (isBearishOb)
                        {
                            // Checks if a subsequent candle entered or touched the lower boundary of the OB. [Ref: Bearish-OB-Touch]
                            if (futureHigh >= obLow && !isTested)
                            {
                                // Flags initial test as active. [Ref: OB-Mark-Tested]
                                isTested = true;
                                // Records exact test timestamp. [Ref: OB-Log-Test-Timestamp]
                                testTime = futureCandle.Timestamp;
                            }

                            // Checks if the block has been invalidated (price closed above the invalidation boundary/high of OB). [Ref: Bearish-OB-Invalidation]
                            if (futureClose > obHigh)
                            {
                                // Flags block as invalidated. [Ref: OB-Mark-Invalidated]
                                isInvalidated = true;
                                // Breaks out of forward scan since the block is dead. [Ref: OB-Scan-Break]
                                break;
                            }
                        }
                    }

                    // Calculates significance score using breakout candle range and relative volume. [Ref: OB-Quality-Scoring]
                    decimal boRange = boClose > boOpen ? boClose - boOpen : boOpen - boClose;
                    decimal boVolume = (decimal)breakoutCandle.Volume.Value;
                    decimal obVolume = (decimal)obCandle.Volume.Value;
                    // Composite score: represents the momentum force of the breakout and institutional volume. [Ref: Composite-Formula]
                    decimal significanceScore = boRange * (boVolume + obVolume);

                    // Compiles calculated parameters into an immutable Order Block record object. [Ref: Compile-Ob]
                    var ob = new OrderBlock(
                        Timestamp: obCandle.Timestamp,
                        HighBoundary: obHigh,
                        LowBoundary: obLow,
                        OpenPrice: obOpen,
                        ClosePrice: obClose,
                        Type: isBullishOb ? OrderBlockType.Bullish : OrderBlockType.Bearish,
                        IsTested: isTested,
                        IsInvalidated: isInvalidated,
                        TestTimestamp: testTime,
                        SignificanceScore: significanceScore,
                        CandleIndex: i
                    );

                    // Commits the Order Block record to the context storage, updating existing values. [Ref: Commit-Store-OB]
                    obDict.AddOrUpdate(obCandle.Timestamp, ob, (key, existing) => ob);
                }
            }

            // Passes the context up the pipeline with completed Order Block metrics. [Ref: Task-Complete-OB]
            return Task.FromResult(context);
        }
    }
}