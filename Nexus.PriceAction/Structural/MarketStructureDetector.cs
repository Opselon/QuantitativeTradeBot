// Imports essential system namespaces for mathematical computations and dynamic properties. [Ref: Core-Libraries]
using System;
// Imports thread-safe concurrent dictionaries to safely record structure breaks on high-frequency live charts. [Ref: Concurrency-Control]
using System.Collections.Concurrent;
// Imports generic lists and structures for chronological tracking. [Ref: Collections-Generic]
using System.Collections.Generic;
// Imports compiler runtime optimization tools to prevent allocation overflows. [Ref: Native-Optimizations]
using System.Runtime.CompilerServices;
// Imports system thread cancellation tokens to handle abort commands cleanly. [Ref: Thread-Abort]
using System.Threading;
// Imports task async processing wrappers for parallel computing. [Ref: Async-Execution]
using System.Threading.Tasks;
// Imports core project abstractions and pipeline contracts. [Ref: Project-Contracts]
using Nexus.PriceAction.Abstractions;
// Maps the core domain Candle entity to bypass local naming conflicts. [Ref: Domain-Mapping]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the high-performance structural price action analysis namespace. [Ref: SMC-Pro]
namespace Nexus.PriceAction.Structural
{
    // Declares the active trend direction calculated dynamically by the structure state machine. [Ref: Trend-State-Enum]
    public enum TrendDirection
    {
        // No defined structural trend is currently confirmed by the state machine. [Ref: Trend-Undefined]
        Undefined = 0,
        // The market is consistently forming higher highs and higher lows. [Ref: Trend-Bullish]
        Bullish = 1,
        // The market is consistently forming lower highs and lower lows. [Ref: Trend-Bearish]
        Bearish = 2
    }

    // Declares the mechanical type of structure break detected in the candle feed. [Ref: Structure-Break-Enum]
    public enum StructureBreakType
    {
        // Break of Structure: Trend continuation signal confirming dominance. [Ref: Break-BOS]
        Bos = 1,
        // Change of Character: First structural violation signaling a counter-trend reversal. [Ref: Break-CHoCH]
        Choch = 2
    }

    // Encapsulates the immutable quantitative coordinates of a validated structural break event. [Ref: Structure-Break-Record]
    public record StructureBreak(
        // The precise timestamp of the breakout candle that closed beyond the key structural boundary. [Ref: Break-Timestamp]
        DateTime Timestamp,
        // The exact close price of the breakout candle confirming physical structure violation. [Ref: Breakout-Price]
        decimal BreakoutPrice,
        // The price coordinate of the broken Swing High or Swing Low level. [Ref: Broken-Level-Price]
        decimal BrokenLevelPrice,
        // The historical timestamp when the broken Swing level was originally formed. [Ref: Broken-Level-Timestamp]
        DateTime BrokenLevelTimestamp,
        // Specifies if the structural break represents a BOS (Continuation) or a CHoCH (Reversal). [Ref: Break-Category]
        StructureBreakType BreakType,
        // The updated trend direction resulting from this structural shift. [Ref: Post-Break-Trend]
        TrendDirection ResultingTrend,
        // The sequential index of the breakout candle within the current buffer. [Ref: Break-Chrono-Index]
        int CandleIndex
    );

    // High-performance state-machine engine to dynamically track trends, BOS, and CHoCH events. [Ref: SRP-MarketStructure]
    public class MarketStructureDetector : IPriceActionEngine
    {
        // Thread-safely maps pipeline contexts to their respective structure break registries. [Ref: Break-Cache-Registry]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, StructureBreak>> BreakProCache = new();

        // Thread-safely maps pipeline contexts to their active trend direction states. [Ref: Trend-Cache-Registry]
        private static readonly ConditionalWeakTable<PriceActionContext, StrongBox<TrendDirection>> TrendProCache = new();

        // Public query API to retrieve a computed structure break from context memory safely. [Ref: Break-Public-Query]
        public static StructureBreak? GetStructureBreak(PriceActionContext context, DateTime timestamp)
        {
            // Tries to locate the context instance within the thread-safe dynamic weak reference table. [Ref: WeakTable-Query-Breaks]
            if (BreakProCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed Structure Break record if registered under the given timestamp key. [Ref: Retrieve-Break-Record]
                if (dictionary.TryGetValue(timestamp, out var sb))
                {
                    // Returns the successfully retrieved structure break object. [Ref: Return-Break-Value]
                    return sb;
                }
            }
            // Returns null if the specified timestamp has no valid structure break registered. [Ref: Fallback-Break-Null]
            return null;
        }

        // Public API to extract all registered Structure Breaks sorted chronologically. [Ref: Break-Bulk-Extraction]
        public static IReadOnlyList<StructureBreak> GetAllStructureBreaks(PriceActionContext context)
        {
            // Locates the specific context store inside the optimized caching memory table. [Ref: Partition-Extract-Breaks]
            if (BreakProCache.TryGetValue(context, out var dictionary))
            {
                // Takes an atomic snapshot list of all computed Structure Break records. [Ref: Snapshot-Copy-Breaks]
                var list = new List<StructureBreak>(dictionary.Values);
                // Sorts the list chronologically according to the breakout index sequence. [Ref: Sequence-Sorting-Breaks]
                list.Sort((x, y) => x.CandleIndex.CompareTo(y.CandleIndex));
                // Exposes the sorted sequence as a clean read-only array to prevent external mutations. [Ref: Safe-Expose-Breaks]
                return list;
            }
            // Returns an empty list to prevent downstream processing errors. [Ref: Null-Safe-Exit-Breaks]
            return Array.Empty<StructureBreak>();
        }

        // Public API to query the active trend direction state of a specific context. [Ref: Trend-Public-Query]
        public static TrendDirection GetCurrentTrend(PriceActionContext context)
        {
            // Tries to locate the active trend reference box from context memory. [Ref: WeakTable-Query-Trend]
            if (TrendProCache.TryGetValue(context, out var trendBox))
            {
                // Returns the inner value from the strong box wrapper. [Ref: Return-Boxed-Trend]
                return trendBox.Value;
            }
            // Returns Undefined if no active trend state is registered yet. [Ref: Fallback-Trend]
            return TrendDirection.Undefined;
        }

        // Asynchronously calculates market structure breaks and shifts using historical swing pivots. [Ref: Main-Pipeline-Structure]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Guards against null contexts to protect system boundaries from unhandled errors. [Ref: Input-Guard-Structure]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Thread-safely obtains or instantiates the structure break storage collection for this context. [Ref: Store-Acquisition-Breaks]
            var breakDict = BreakProCache.GetOrCreateValue(context);
            // Obtains or instantiates the trend tracking box for this context. [Ref: Store-Acquisition-Trend]
            var trendBox = TrendProCache.GetOrCreateValue(context);

            // Obtains all computed swing points from the first engine phase. [Ref: Get-Swing-Data]
            var swings = SwingPointDetector.GetAllSwingPoints(context);

            // Obtains the physical size of the raw price feed buffer. [Ref: Feed-Extraction-Structure]
            int candleCount = context.RawCandles.Count;

            // Halts execution if the historical feed lacks the minimum data threshold for analysis. [Ref: Minimum-Feed-Check-Structure]
            if (candleCount < 5 || swings.Count == 0)
            {
                // Safely passes back the untouched context through the async pipeline wrapper. [Ref: Early-Pass-Structure]
                return Task.FromResult(context);
            }

            // Initializes state machine trackers for trend evaluation. [Ref: State-Machine-Init]
            TrendDirection currentTrend = TrendDirection.Undefined;
            // Keeps track of the most recent confirmed swing high pivot. [Ref: Swing-Pivot-High-Ref]
            SwingPoint? lastSwingHigh = null;
            // Keeps track of the most recent confirmed swing low pivot. [Ref: Swing-Pivot-Low-Ref]
            SwingPoint? lastSwingLow = null;

            // Creates a fast lookup mapping of swing points keyed by index for O(1) correlation checks during index loops. [Ref: Swing-Index-Map]
            var swingLookup = new Dictionary<int, SwingPoint>();
            // Populates index lookup map. [Ref: Swing-Map-Loop]
            foreach (var swing in swings)
            {
                // Registers swing point. [Ref: Swing-Map-Add]
                swingLookup[swing.Index] = swing;
            }

            // Sequentially traverses candles chronologically to simulate live state transition logic. [Ref: Chrono-Traversal-Structure]
            for (int i = 0; i < candleCount; i++)
            {
                // Enables dynamic thread context interrupting to manage high system utilization. [Ref: Thread-Check-Structure]
                cancellationToken.ThrowIfCancellationRequested();

                // Resolves if a new validated swing point formed at the current index. [Ref: Check-New-Swing]
                if (swingLookup.TryGetValue(i, out var newSwing))
                {
                    // Assigns pivot based on type. [Ref: Pivot-Assignment]
                    if (newSwing.IsHigh)
                    {
                        // Updates last swing high pivot reference. [Ref: Update-Swing-High]
                        lastSwingHigh = newSwing;
                    }
                    else
                    {
                        // Updates last swing low pivot reference. [Ref: Update-Swing-Low]
                        lastSwingLow = newSwing;
                    }

                    // Checks if we can establish an initial trend direction from early swing pairs. [Ref: Initial-Trend-Check]
                    if (currentTrend == TrendDirection.Undefined && lastSwingHigh != null && lastSwingLow != null)
                    {
                        // Bullish if the last high was formed after the low and closed higher. [Ref: Bullish-Condition]
                        currentTrend = lastSwingHigh.Index > lastSwingLow.Index ? TrendDirection.Bullish : TrendDirection.Bearish;
                        // Saves updated trend into strong box reference. [Ref: Save-Boxed-Initial-Trend]
                        trendBox.Value = currentTrend;
                    }
                }

                // References the current testing candle. [Ref: Structure-Test-Candle]
                DomainCandle currentCandle = context.RawCandles[i];
                decimal closePrice = (decimal)currentCandle.Close.Value;

                // Proceeds only if the initial trend direction is confirmed. [Ref: Trend-Confirmed-Guard]
                if (currentTrend == TrendDirection.Bullish)
                {
                    // Bullish Continuation Check (BOS): Price closes above the most recent confirmed Swing High. [Ref: Bullish-Continuation-Math]
                    if (lastSwingHigh != null && closePrice > lastSwingHigh.Price && i > lastSwingHigh.Index)
                    {
                        // Compiles validated BOS parameters. [Ref: Compile-Bullish-BOS]
                        var bos = new StructureBreak(
                            Timestamp: currentCandle.Timestamp,
                            BreakoutPrice: closePrice,
                            BrokenLevelPrice: lastSwingHigh.Price,
                            BrokenLevelTimestamp: lastSwingHigh.Timestamp,
                            BreakType: StructureBreakType.Bos,
                            ResultingTrend: TrendDirection.Bullish,
                            CandleIndex: i
                        );

                        // Registers the BOS event in context memory. [Ref: Commit-BOS-Record]
                        breakDict.AddOrUpdate(currentCandle.Timestamp, bos, (key, existing) => bos);

                        // Resets broken swing high to prevent duplicate triggers on the same pivot. [Ref: Pivot-Reset-High]
                        lastSwingHigh = null;
                    }

                    // Bullish Reversal Check (CHoCH): Price closes below the most recent confirmed Swing Low. [Ref: Bullish-Reversal-Math]
                    if (lastSwingLow != null && closePrice < lastSwingLow.Price && i > lastSwingLow.Index)
                    {
                        // Updates market trend state to bearish. [Ref: Flip-Trend-To-Bearish]
                        currentTrend = TrendDirection.Bearish;
                        // Saves updated trend into strong box reference. [Ref: Save-Boxed-Trend]
                        trendBox.Value = currentTrend;

                        // Compiles validated CHoCH parameters. [Ref: Compile-Bullish-CHoCH]
                        var choch = new StructureBreak(
                            Timestamp: currentCandle.Timestamp,
                            BreakoutPrice: closePrice,
                            BrokenLevelPrice: lastSwingLow.Price,
                            BrokenLevelTimestamp: lastSwingLow.Timestamp,
                            BreakType: StructureBreakType.Choch,
                            ResultingTrend: TrendDirection.Bearish,
                            CandleIndex: i
                        );

                        // Registers the CHoCH event in context memory. [Ref: Commit-CHoCH-Record]
                        breakDict.AddOrUpdate(currentCandle.Timestamp, choch, (key, existing) => choch);

                        // Resets broken swing low. [Ref: Pivot-Reset-Low]
                        lastSwingLow = null;
                    }
                }
                else if (currentTrend == TrendDirection.Bearish)
                {
                    // Bearish Continuation Check (BOS): Price closes below the most recent confirmed Swing Low. [Ref: Bearish-Continuation-Math]
                    if (lastSwingLow != null && closePrice < lastSwingLow.Price && i > lastSwingLow.Index)
                    {
                        // Compiles validated BOS parameters. [Ref: Compile-Bearish-BOS]
                        var bos = new StructureBreak(
                            Timestamp: currentCandle.Timestamp,
                            BreakoutPrice: closePrice,
                            BrokenLevelPrice: lastSwingLow.Price,
                            BrokenLevelTimestamp: lastSwingLow.Timestamp,
                            BreakType: StructureBreakType.Bos,
                            ResultingTrend: TrendDirection.Bearish,
                            CandleIndex: i
                        );

                        // Registers the BOS event in context memory. [Ref: Commit-BOS-Record-Bearish]
                        breakDict.AddOrUpdate(currentCandle.Timestamp, bos, (key, existing) => bos);

                        // Resets broken swing low. [Ref: Pivot-Reset-Low-Bearish]
                        lastSwingLow = null;
                    }

                    // Bearish Reversal Check (CHoCH): Price closes above the most recent confirmed Swing High. [Ref: Bearish-Reversal-Math]
                    if (lastSwingHigh != null && closePrice > lastSwingHigh.Price && i > lastSwingHigh.Index)
                    {
                        // Updates market trend state to bullish. [Ref: Flip-Trend-To-Bullish]
                        currentTrend = TrendDirection.Bullish;
                        // Saves updated trend into strong box reference. [Ref: Save-Boxed-Trend-Bullish]
                        trendBox.Value = currentTrend;

                        // Compiles validated CHoCH parameters. [Ref: Compile-Bearish-CHoCH]
                        var choch = new StructureBreak(
                            Timestamp: currentCandle.Timestamp,
                            BreakoutPrice: closePrice,
                            BrokenLevelPrice: lastSwingHigh.Price,
                            BrokenLevelTimestamp: lastSwingHigh.Timestamp,
                            BreakType: StructureBreakType.Choch,
                            ResultingTrend: TrendDirection.Bullish,
                            CandleIndex: i
                        );

                        // Registers the CHoCH event in context memory. [Ref: Commit-CHoCH-Record-Bearish]
                        breakDict.AddOrUpdate(currentCandle.Timestamp, choch, (key, existing) => choch);

                        // Resets broken swing high. [Ref: Pivot-Reset-High-Bearish]
                        lastSwingHigh = null;
                    }
                }
            }

            // Passes the context up the pipeline with completed Structure Breaks and trend states. [Ref: Task-Complete-Structure]
            return Task.FromResult(context);
        }
    }
}