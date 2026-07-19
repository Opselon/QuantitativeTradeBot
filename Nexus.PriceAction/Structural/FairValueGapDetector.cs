// Imports fundamental systems libraries for basic logical and mathematical operations. [Ref: Core-Libraries]
// Imports thread-safe concurrent collections to avoid state corruption during multithreaded analysis. [Ref: Concurrency-Control]
// Imports multi-threading cancellation signals to handle forced runtime shutdowns gracefully. [Ref: Thread-Abort]
// Imports asynchronous task wrappers to delegate execution to ThreadPool worker threads. [Ref: Async-Execution]
// Imports project price action orchestrator contracts to guarantee structural compatibility. [Ref: Project-Contracts]
using Nexus.PriceAction.Abstractions;
using System.Collections.Concurrent;
// Imports specialized collections representing sequence configurations and historical records. [Ref: Collections-Generic]
// Imports compiler optimization services to enable zero-overhead memory and execution caching. [Ref: Native-Optimizations]
using System.Runtime.CompilerServices;
// Maps the core domain Candle model under an alias to eliminate type resolution ambiguities. [Ref: Domain-Mapping]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the high-frequency structural price action analysis namespace. [Ref: SMC-Pro]
namespace Nexus.PriceAction.Structural
{
    // Represents the directional behavior of the detected market imbalance. [Ref: Imbalance-Direction]
    public enum FvgType
    {
        // Bullish gap created by violent upward buying pressure with unresolved liquidity. [Ref: Direction-Bullish]
        Bullish = 1,
        // Bearish gap created by violent downward selling pressure with unresolved liquidity. [Ref: Direction-Bearish]
        Bearish = 2
    }

    // Encapsulates the immutable quantitative and mechanical metrics of a verified Fair Value Gap. [Ref: Quant-State]
    public record FairValueGap(
        // The timestamp of the central impulsive candle (Candle 2 in the 3-candle sequence). [Ref: Trigger-Time]
        DateTime Timestamp,
        // The upper horizontal boundary level of the structural imbalance zone. [Ref: Zone-Top]
        decimal UpperBoundary,
        // The lower horizontal boundary level of the structural imbalance zone. [Ref: Zone-Bottom]
        decimal LowerBoundary,
        // The original vertical height of the gap measured in decimal quote units. [Ref: Original-Size]
        decimal OriginalSize,
        // The remaining unmitigated vertical gap height left exposed to future test sweeps. [Ref: Remaining-Exposure]
        decimal RemainingSize,
        // The directional category (Bullish/Bearish) determining execution bias. [Ref: Direction-Type]
        FvgType Type,
        // Indicates if any future candle wick has touched or entered this imbalance zone. [Ref: Touch-Status]
        bool IsMitigated,
        // Indicates if future candles have completely closed beyond the boundaries of this gap. [Ref: Complete-Fill]
        bool IsFullyMitigated,
        // The exact timestamp when this FVG was fully resolved or cancelled by price. [Ref: Resolution-Time]
        DateTime? MitigationTimestamp,
        // The exact price recorded at the moment of complete mitigation. [Ref: Resolution-Price]
        decimal MitigationPrice,
        // The relative trading volume of Candle 2, scoring institutional commitment. [Ref: Volume-Score]
        decimal VolumeScore,
        // The sequential index of Candle 2 in the chronological data feed. [Ref: Chrono-Index]
        int CenterIndex
    );

    // High-performance non-blocking engine to calculate, track, and mitigate FVGs dynamically. [Ref: SRP-Imbalance]
    public class FairValueGapDetector : IPriceActionEngine
    {
        // Dynamic WeakTable reference to cache imbalance states bound to context lifespans. [Ref: Cache-Registry]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, FairValueGap>> FvgProCache = new();

        // Historical window length used to calculate Average True Range for volatility thresholds. [Ref: Volatility-Period]
        private const int AtrPeriod = 14;

        // Volatility multiplier acting as a sensitivity gate to filter out negligible micro-imbalances. [Ref: Sensitivity-Gate]
        private const decimal VolatilityMultiplier = 1.2m;

        // Public API to query the active FVG profile for a specific candle with safe nullable annotations. [Ref: Public-Query-Nullable]
        public static FairValueGap? GetFvg(PriceActionContext context, DateTime timestamp)
        {
            // Tries to locate the context instance within the thread-safe dynamic weak reference table. [Ref: WeakTable-Query]
            if (FvgProCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed FVG record if registered under the given timestamp key. [Ref: Retrieve-Record]
                if (dictionary.TryGetValue(timestamp, out var fvg))
                {
                    // Returns the successfully retrieved imbalance data object. [Ref: Return-Value]
                    return fvg;
                }
            }
            // Safely returns null since the return signature is now explicitly declared as nullable. [Ref: Safe-Null-Return]
            return null;
        }

        // Public API to extract all registered Fair Value Gaps sorted chronologically. [Ref: Bulk-Extraction]
        public static IReadOnlyList<FairValueGap> GetAllFvgs(PriceActionContext context)
        {
            // Locates the specific context store inside the optimized caching memory table. [Ref: Partition-Extract]
            if (FvgProCache.TryGetValue(context, out var dictionary))
            {
                // Takes an atomic snapshot list of all computed FVG records. [Ref: Snapshot-Copy]
                var list = new List<FairValueGap>(dictionary.Values);
                // Sorts the list chronologically according to the center index sequence. [Ref: Sequence-Sorting]
                list.Sort((x, y) => x.CenterIndex.CompareTo(y.CenterIndex));
                // Exposes the sorted sequence as a clean read-only array to prevent external mutations. [Ref: Safe-Expose]
                return list;
            }
            // Returns an empty list to prevent downstream processing errors. [Ref: Null-Safe-Exit]
            return Array.Empty<FairValueGap>();
        }

        // Asynchronously executes high-performance FVG detection and mitigation algorithms over candles. [Ref: Main-Pipeline]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Guards against null contexts to protect system boundaries from unhandled errors. [Ref: Input-Guard]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Thread-safely obtains or instantiates the imbalance storage collection for this context. [Ref: Store-Acquisition]
            var fvgDict = FvgProCache.GetOrCreateValue(context);

            // Measures the current size of the historical candle feed buffer. [Ref: Buffer-Size]
            int candleCount = context.RawCandles.Count;

            // Demands a minimum of three candles to construct the base imbalance sequence pattern. [Ref: Sequence-Guard]
            if (candleCount < 3)
            {
                // Exits immediately if data is insufficient for standard 3-candle evaluations. [Ref: Early-Pass]
                return Task.FromResult(context);
            }

            // Loops chronologically from index 1 to count-2 to evaluate every possible central candle. [Ref: Chrono-Traversal]
            for (int i = 1; i < candleCount - 1; i++)
            {
                // Enables dynamic thread context interrupting to manage high system utilization. [Ref: Thread-Check]
                cancellationToken.ThrowIfCancellationRequested();

                // References the preceding candle (Candle 1 in the 3-bar structural pattern). [Ref: Candle-One]
                DomainCandle c1 = context.RawCandles[i - 1];
                // References the central explosive candle (Candle 2 in the 3-bar structural pattern). [Ref: Candle-Two]
                DomainCandle c2 = context.RawCandles[i];
                // References the succeeding candle (Candle 3 in the 3-bar structural pattern). [Ref: Candle-Three]
                DomainCandle c3 = context.RawCandles[i + 1];

                // Converts boundary levels of Candle 1 into standard decimal coordinates. [Ref: C1-Prices]
                decimal c1High = (decimal)c1.High.Value;
                // Converts bottom boundary level of Candle 1. [Ref: C1-Low]
                decimal c1Low = (decimal)c1.Low.Value;

                // Converts boundary levels of Candle 2 (the middle explosive candle). [Ref: C2-Prices]
                decimal c2High = (decimal)c2.High.Value;
                // Converts bottom boundary level of Candle 2. [Ref: C2-Low]
                decimal c2Low = (decimal)c2.Low.Value;

                // Converts boundary levels of Candle 3 (the confirmation candle). [Ref: C3-Prices]
                decimal c3High = (decimal)c3.High.Value;
                // Converts bottom boundary level of Candle 3. [Ref: C3-Low]
                decimal c3Low = (decimal)c3.Low.Value;

                // Computes rolling Average True Range to act as a dynamic volatility filter. [Ref: Volatility-Evaluation]
                decimal atr = CalculateAtr(context, i);

                // Computes the absolute trading range of the central impulsive candle. [Ref: Impulse-Magnitude]
                decimal c2Range = c2High - c2Low;

                // Filters out minor gaps formed inside squeezed consolidation ranges. [Ref: Consolidation-Filter]
                if (c2Range < atr * VolatilityMultiplier)
                {
                    // Skips the current candle sequence due to inadequate volatility magnitude. [Ref: Skip-Contraction]
                    continue;
                }

                // Initializes FVG calculation structures. [Ref: Struct-Init]
                bool isBullishFvg = c3Low > c1High;
                // Evaluates if the sequence matches structural Bearish FVG criteria. [Ref: Bearish-Logic-Check]
                bool isBearishFvg = c1Low > c3High;

                // Proceeds only if a valid directional gap is confirmed in the sequence. [Ref: Imbalance-Check]
                if (isBullishFvg || isBearishFvg)
                {
                    // Determines boundaries based on structural type. [Ref: Boundary-Assignment]
                    decimal upperBoundary = isBullishFvg ? c3Low : c1Low;
                    // Assigns bottom boundary. [Ref: Bottom-Assignment]
                    decimal lowerBoundary = isBullishFvg ? c1High : c3High;

                    // Calculates the total original height of the unresolved liquidity void. [Ref: Original-Height-Math]
                    decimal originalSize = upperBoundary - lowerBoundary;

                    // Instantiates mitigation tracking states before scanning future price actions. [Ref: Mitigation-Prep]
                    bool isMitigated = false;
                    // Prepares complete fill flag. [Ref: Complete-Fill-Prep]
                    bool isFullyMitigated = false;
                    // Declares mitigation date storage. [Ref: Date-Decl]
                    DateTime? mitigationTime = null;
                    // Declares resolution price tracking coordinate. [Ref: Price-Decl]
                    decimal mitigationPrice = 0m;
                    // Sets default remaining size to original size prior to historical search. [Ref: Size-Reset]
                    decimal remainingSize = originalSize;

                    // Scans forward from Candle 4 to the current buffer end to track mitigation decay. [Ref: Forward-Decay-Scan]
                    for (int j = i + 2; j < candleCount; j++)
                    {
                        // References the subsequent candle testing the imbalance zone. [Ref: Test-Candle]
                        DomainCandle futureCandle = context.RawCandles[j];
                        // Obtains future high boundary. [Ref: Future-High]
                        decimal futureHigh = (decimal)futureCandle.High.Value;
                        // Obtains future low boundary. [Ref: Future-Low]
                        decimal futureLow = (decimal)futureCandle.Low.Value;

                        // Evaluates mitigation logic for Bullish Fair Value Gaps. [Ref: Bullish-Mitigation-Logic]
                        if (isBullishFvg)
                        {
                            // Checks if a subsequent candle wick penetrated into the gap. [Ref: Bullish-Penetration]
                            if (futureLow < upperBoundary)
                            {
                                // Flags initial mitigation as active. [Ref: Mark-Mitigated]
                                isMitigated = true;
                                // Calculates remaining unmitigated gap height. [Ref: Remaining-Height-Math]
                                remainingSize = Math.Max(0m, futureLow - lowerBoundary);

                                // Checks if the gap has been completely filled and resolved. [Ref: Bullish-Resolution-Check]
                                if (futureLow <= lowerBoundary)
                                {
                                    // Flags complete mitigation resolution. [Ref: Mark-Resolved]
                                    isFullyMitigated = true;
                                    // Records exact mitigation timestamp. [Ref: Log-Timestamp]
                                    mitigationTime = futureCandle.Timestamp;
                                    // Logs resolution price. [Ref: Log-Price]
                                    mitigationPrice = lowerBoundary;
                                    // Breaks out of forward decay scan since FVG is fully resolved. [Ref: Decay-Break]
                                    break;
                                }
                            }
                        }
                        // Evaluates mitigation logic for Bearish Fair Value Gaps. [Ref: Bearish-Mitigation-Logic]
                        else if (isBearishFvg)
                        {
                            // Checks if a subsequent candle wick penetrated up into the gap. [Ref: Bearish-Penetration]
                            if (futureHigh > lowerBoundary)
                            {
                                // Flags initial bearish mitigation. [Ref: Mark-Mitigated]
                                isMitigated = true;
                                // Calculates remaining unmitigated gap height. [Ref: Remaining-Height-Math]
                                remainingSize = Math.Max(0m, upperBoundary - futureHigh);

                                // Checks if the bearish gap was completely closed by buying pressure. [Ref: Bearish-Resolution-Check]
                                if (futureHigh >= upperBoundary)
                                {
                                    // Flags complete bearish mitigation. [Ref: Mark-Resolved]
                                    isFullyMitigated = true;
                                    // Records exact bearish resolution timestamp. [Ref: Log-Timestamp]
                                    mitigationTime = futureCandle.Timestamp;
                                    // Logs resolution price. [Ref: Log-Price]
                                    mitigationPrice = upperBoundary;
                                    // Breaks forward decay scan for bearish gap. [Ref: Decay-Break]
                                    break;
                                }
                            }
                        }
                    }

                    // Extracts relative volume score representing institutional backing of the gap. [Ref: Institutional-Volume]
                    decimal volumeScore = (decimal)c2.Volume.Value;

                    // Compiles calculated parameters into an immutable FVG record object. [Ref: Compile-Fvg]
                    var fvg = new FairValueGap(
                        Timestamp: c2.Timestamp,
                        UpperBoundary: upperBoundary,
                        LowerBoundary: lowerBoundary,
                        OriginalSize: originalSize,
                        RemainingSize: remainingSize,
                        Type: isBullishFvg ? FvgType.Bullish : FvgType.Bearish,
                        IsMitigated: isMitigated,
                        IsFullyMitigated: isFullyMitigated,
                        MitigationTimestamp: mitigationTime,
                        MitigationPrice: mitigationPrice,
                        VolumeScore: volumeScore,
                        CenterIndex: i
                    );

                    // Commits the FVG record to the context storage, updating existing values. [Ref: Commit-Store]
                    fvgDict.AddOrUpdate(c2.Timestamp, fvg, (key, existing) => fvg);
                }
            }

            // Passes the context up the pipeline with completed FVG and decay metrics. [Ref: Task-Complete]
            return Task.FromResult(context);
        }

        // Calculates standard Average True Range over a rolling historical sequence. [Ref: Volatility-ATR]
        private decimal CalculateAtr(PriceActionContext context, int currentIndex)
        {
            // Resolves early window bounds to prevent out-of-index exceptions. [Ref: Safe-Start]
            int startIndex = Math.Max(1, currentIndex - AtrPeriod);
            // Stores count of processed ranges. [Ref: Range-Count]
            int count = currentIndex - startIndex + 1;

            // Accumulates true range values across the current period. [Ref: Accumulation-Loop]
            decimal trSum = 0m;
            // Iterates through historical window. [Ref: Atr-Loop]
            for (int k = startIndex; k <= currentIndex; k++)
            {
                // Current candle ref. [Ref: Current]
                DomainCandle current = context.RawCandles[k];
                // Prior candle ref. [Ref: Prior]
                DomainCandle prior = context.RawCandles[k - 1];

                // Converts boundaries. [Ref: Bounds]
                decimal curHigh = (decimal)current.High.Value;
                decimal curLow = (decimal)current.Low.Value;
                decimal priClose = (decimal)prior.Close.Value;

                // Computes the three potential components of True Range. [Ref: True-Range-Components]
                decimal hMinusL = curHigh - curLow;
                decimal hMinusPc = Math.Abs(curHigh - priClose);
                decimal lMinusPc = Math.Abs(curLow - priClose);

                // Identifies the absolute maximum value representing the True Range. [Ref: Max-True-Range]
                decimal trueRange = Math.Max(hMinusL, Math.Max(hMinusPc, lMinusPc));
                // Sums the values. [Ref: Accumulate]
                trSum += trueRange;
            }

            // Returns the simple moving average representing volatility. [Ref: ATR-Final-Return]
            return trSum / count;
        }
    }
}