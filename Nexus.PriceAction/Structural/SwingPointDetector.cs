// Imports essential systems logic for structural mathematics and execution. [Ref: Core-Libraries]
using System;
// Imports atomic thread-safe concurrent dictionaries to handle live streaming safely. [Ref: Concurrency-Control]
using System.Collections.Concurrent;
// Imports system generic collections for structural buffers and pipelines. [Ref: Generic-Collections]
using System.Collections.Generic;
// Imports compiler runtime optimization tools to prevent object allocations on memory gc. [Ref: Native-Optimizations]
using System.Runtime.CompilerServices;
// Imports thread cancellation abstractions to abort heavy loops dynamically. [Ref: Thread-Abort]
using System.Threading;
// Imports async task paradigms for parallel high-frequency computation threads. [Ref: Async-Execution]
using System.Threading.Tasks;
// Imports price action core contracts to align with onion architecture boundaries. [Ref: Project-Contracts]
using Nexus.PriceAction.Abstractions;
// Maps the core immutable Candle domain model to prevent namespace collisions. [Ref: Domain-Mapping]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the high-performance structural price action analysis namespace. [Ref: SMC-Pro]
namespace Nexus.PriceAction.Structural
{
    // Represents the structural depth hierarchy of swing points in institutional trading. [Ref: Depth-Levels]
    public enum StructureDepth
    {
        // Internal minor structure swing point, used for intraday liquidity sweeps. [Ref: Level-Minor]
        Minor = 1,
        // Medium structural swings representing intermediate consolidation boundaries. [Ref: Level-Medium]
        Medium = 2,
        // Major swing points defining the macro trend boundaries and key execution pools. [Ref: Level-Major]
        Major = 3
    }

    // Encapsulates a highly advanced mathematical state of a validated swing point. [Ref: Quant-State]
    public record SwingPoint(
        // The precise chronological occurrence timestamp of the structural peak/trough. [Ref: Audit-Time]
        DateTime Timestamp,
        // The absolute decimal price level of the structural vertex. [Ref: Price-Vertex]
        decimal Price,
        // Denotes whether this swing represents a high resistance peak (True) or a low support trough (False). [Ref: Direction-Flag]
        bool IsHigh,
        // Indicates if the right-hand confirmation window has fully closed and verified the point. [Ref: Live-Safety]
        bool IsConfirmed,
        // The absolute sequential index of the source candle within the current memory partition. [Ref: Index-Tracking]
        int Index,
        // The structural significance category representing market influence. [Ref: Structure-Categorization]
        StructureDepth Depth,
        // Normalized quantitative strength score based on relative volume and candle magnitude. [Ref: Strength-Metric]
        decimal SignificanceScore
    );

    // Advanced non-blocking engine to dynamically detect and score multi-depth market swing structures. [Ref: SRP-Pro]
    public class SwingPointDetector : IPriceActionEngine
    {
        // Thread-safely caches dynamic multi-depth swing dictionaries to target context instances. [Ref: WeakTable-Cache]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, SwingPoint>> SwingProCache = new();

        // Standard lookup window size to calculate standard deviation and historical ranges. [Ref: Math-Window]
        private const int VolatilityWindow = 20;

        // Retrieves a registered swing point for a given timestamp if it exists in current pipeline memory. [Ref: Query-API]
        // Public helper method with safe nullable signature to check if a swing point exists. [Ref: Public-API-Query-Nullable]
        public static SwingPoint? GetSwingPoint(PriceActionContext context, DateTime timestamp)
        {
            // Tries to retrieve the active swing dictionary registered to current context. [Ref: Cache-Lookup]
            if (SwingProCache.TryGetValue(context, out var dictionary))
            {
                // Extracts the swing point value if present under the target key. [Ref: Concurrent-Retrieve]
                if (dictionary.TryGetValue(timestamp, out var point))
                {
                    // Returns the validated swing point metadata. [Ref: Result-Pass]
                    return point;
                }
            }
            // Safely returns null since the method signature is explicitly decorated as nullable. [Ref: Safe-Null-Return]
            return null;
        }

        // Extracts all detected structural points in chronological sequence for charting or pattern matching. [Ref: Analytical-API]
        public static IReadOnlyList<SwingPoint> GetAllSwingPoints(PriceActionContext context)
        {
            // Looks up the specific context database inside the optimized memory lookup table. [Ref: Table-Lookup]
            if (SwingProCache.TryGetValue(context, out var dictionary))
            {
                // Creates a thread-safe snapshot collection of the currently calculated swing points. [Ref: Snapshot-Copy]
                var list = new List<SwingPoint>(dictionary.Values);
                // Sorts the list chronologically based on sequential index representation. [Ref: Chrono-Sort]
                list.Sort((x, y) => x.Index.CompareTo(y.Index));
                // Exposes the sorted array as an immutable read-only sequence. [Ref: Safe-Expose]
                return list;
            }
            // Returns an empty array if no context structure is registered to avoid null-reference crashes. [Ref: Null-Safe]
            return Array.Empty<SwingPoint>();
        }

        // Asynchronously orchestrates high-performance multi-depth structural scans across candle arrays. [Ref: Orchestrated-Scan]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Enforces strict input validation to protect the engine boundary from anomalous execution. [Ref: Input-Guard]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Locates or registers a dedicated concurrent structure cache for this unique context lifespan. [Ref: Lazy-Registry]
            var swingDict = SwingProCache.GetOrCreateValue(context);

            // Obtains the physical size of the raw price feeds under analytical processing. [Ref: Feed-Extraction]
            int candleCount = context.RawCandles.Count;

            // Halts execution if the historical feed lacks the minimum mathematical threshold for basic fractal scanning. [Ref: Minimum-Feed-Check]
            if (candleCount < 7)
            {
                // Safely passes back the untouched context through the async pipeline wrapper. [Ref: Early-Pass]
                return Task.FromResult(context);
            }

            // Loops through chronological candles starting from the safety offset of the largest structural wing. [Ref: Multi-Depth-Loop]
            for (int i = 3; i < candleCount; i++)
            {
                // Enables immediate thread interruption during extreme high-frequency load conditions. [Ref: Concurrency-Safety]
                cancellationToken.ThrowIfCancellationRequested();

                // Evaluates the target index for Minor (strength=1), Medium (strength=2), and Major (strength=3) structural depths. [Ref: Depth-Loop]
                for (int depthLevel = 3; depthLevel >= 1; depthLevel--)
                {
                    // Assigns the temporary strength modifier dynamically based on structural hierarchy. [Ref: Strength-Mapping]
                    int strength = depthLevel;

                    // Prevents scanning past left boundary indices of the current window configuration. [Ref: Left-Boundary-Guard]
                    if (i < strength) continue;

                    // Extracts the candidate candle for validation at the central index. [Ref: Target-Candle]
                    DomainCandle candidate = context.RawCandles[i];

                    // Obtains the absolute high price converted to standard decimal precision. [Ref: Precision-Convert]
                    decimal candidateHigh = (decimal)candidate.High.Value;

                    // Obtains the absolute low price converted to standard decimal precision. [Ref: Precision-Convert]
                    decimal candidateLow = (decimal)candidate.Low.Value;

                    // Initializes the tracking flags for both structural possibilities. [Ref: Flag-Initialization]
                    bool isSwingHigh = true;
                    // Initializes the low tracking flag to check support criteria. [Ref: Support-Flag]
                    bool isSwingLow = true;

                    // Scans the left wing indices to confirm structural dominance over past price action. [Ref: Left-Wing-Validation]
                    for (int left = 1; left <= strength; left++)
                    {
                        // Locates the left relative historical candle index. [Ref: Relative-Index]
                        DomainCandle leftCandle = context.RawCandles[i - left];
                        // Disqualifies swing high if a preceding candle printed an equal or higher peak. [Ref: Left-High-Compare]
                        if ((decimal)leftCandle.High.Value >= candidateHigh) isSwingHigh = false;
                        // Disqualifies swing low if a preceding candle printed an equal or lower trough. [Ref: Left-Low-Compare]
                        if ((decimal)leftCandle.Low.Value <= candidateLow) isSwingLow = false;
                    }

                    // Prepares tracking for right-wing live market validation parameters. [Ref: Live-Safety-Init]
                    bool isConfirmed = true;

                    // Scans the right wing indices to confirm forward-looking price exhaustion. [Ref: Right-Wing-Validation]
                    for (int right = 1; right <= strength; right++)
                    {
                        // Computes the absolute future index within the target list. [Ref: Future-Index-Math]
                        int rightIndex = i + right;

                        // Flags unconfirmed status if future candles have not formed on the exchange server yet. [Ref: Exchange-Lag-Guard]
                        if (rightIndex >= candleCount)
                        {
                            // Marks unconfirmed state representing live, non-finalized price action. [Ref: Unconfirmed-State]
                            isConfirmed = false;
                            // Breaks loop early since future metrics cannot be verified. [Ref: Early-Break]
                            break;
                        }

                        // Locates the future validation candle. [Ref: Future-Candle-Ref]
                        DomainCandle rightCandle = context.RawCandles[rightIndex];
                        // Disqualifies peak status if a succeeding candle exceeded the candidate high. [Ref: Right-High-Compare]
                        if ((decimal)rightCandle.High.Value >= candidateHigh) isSwingHigh = false;
                        // Disqualifies trough status if a succeeding candle breached the candidate low. [Ref: Right-Low-Compare]
                        if ((decimal)rightCandle.Low.Value <= candidateLow) isSwingLow = false;
                    }

                    // Checks if the candidate met all structural validation rules for a peak or trough. [Ref: Structure-Check]
                    if (isSwingHigh || isSwingLow)
                    {
                        // Computes a rolling volatility standard deviation filter to prevent signal noise. [Ref: Noise-Filtering]
                        decimal volatilityThreshold = CalculateVolatilityThreshold(context, i);

                        // Calculates the absolute size of the candidate candle's total trading range. [Ref: Range-Size]
                        decimal candidateRange = (decimal)candidate.High.Value - (decimal)candidate.Low.Value;

                        // Filters out insignificant contractions where range does not exceed volatility threshold. [Ref: Volatility-Gate]
                        if (candidateRange < volatilityThreshold * 0.5m)
                        {
                            // Skips registering this point due to high probability of noise in compressed range. [Ref: Noise-Skip]
                            continue;
                        }

                        // Calculates the significance score of the cycle utilizing volume and range magnitude. [Ref: Quant-Scoring]
                        decimal score = CalculateSignificanceScore(candidate);

                        // Map the integer level back to the domain StructureDepth hierarchy. [Ref: Map-Depth]
                        StructureDepth depth = (StructureDepth)depthLevel;

                        // Compiles the verified swing point parameters into an immutable record. [Ref: Instantiate-Swing]
                        var validatedPoint = new SwingPoint(
                            Timestamp: candidate.Timestamp,
                            Price: isSwingHigh ? candidateHigh : candidateLow,
                            IsHigh: isSwingHigh,
                            IsConfirmed: isConfirmed,
                            Index: i,
                            Depth: depth,
                            SignificanceScore: score
                        );

                        // Adds or updates the registry in a thread-safe manner, keeping the highest depth. [Ref: Upsert-Registry]
                        swingDict.AddOrUpdate(candidate.Timestamp, validatedPoint, (key, existing) =>
                        {
                            // Preserves the existing record if it already captured a higher structural depth. [Ref: Depth-Preservation]
                            return existing.Depth > validatedPoint.Depth ? existing : validatedPoint;
                        });

                        // Breaks out of nested depth loops once the highest matching structural depth is committed. [Ref: Cascade-Break]
                        break;
                    }
                }
            }

            // Passes the structurally enriched context back up to the execution thread pool. [Ref: Task-Complete]
            return Task.FromResult(context);
        }

        // Calculates standard deviation of candle ranges dynamically to filter consolidation noise. [Ref: Math-StdDev]
        private decimal CalculateVolatilityThreshold(PriceActionContext context, int currentIndex)
        {
            // Resolves the dynamic start index based on rolling historical buffer size. [Ref: Rolling-Start]
            int startIndex = Math.Max(0, currentIndex - VolatilityWindow);
            // Determines actual lookback length for mathematical safety against early-feed limits. [Ref: Range-Safety]
            int count = currentIndex - startIndex;

            // Guard clause to prevent division by zero in early history calculations. [Ref: Math-Guard]
            if (count == 0) return 0m;

            // Sums the absolute ranges of the past window sequence. [Ref: Summation]
            decimal rangeSum = 0m;
            // Iterates backward through history to compute variance sum. [Ref: Volatility-Loop]
            for (int k = startIndex; k < currentIndex; k++)
            {
                // Accumulates historical range values. [Ref: Accumulate-Range]
                rangeSum += ((decimal)context.RawCandles[k].High.Value - (decimal)context.RawCandles[k].Low.Value);
            }

            // Calculates the simple mathematical average of candle volatility. [Ref: Mean-Calculation]
            decimal mean = rangeSum / count;
            // Accumulates squared differences from the mean to calculate variance. [Ref: Variance-Accumulation]
            double varianceSum = 0;

            // Computes variance loop. [Ref: Variance-Loop]
            for (int k = startIndex; k < currentIndex; k++)
            {
                // Calculates individual historical range decimal. [Ref: Historic-Range]
                decimal r = ((decimal)context.RawCandles[k].High.Value - (decimal)context.RawCandles[k].Low.Value);
                // Converts difference to double precision. [Ref: Type-Cast]
                double diff = (double)(r - mean);
                // Adds square value. [Ref: Square-Accumulation]
                varianceSum += diff * diff;
            }

            // Calculates absolute standard deviation via square root conversion. [Ref: Square-Root]
            return (decimal)Math.Sqrt(varianceSum / count);
        }

        // Calculates mathematical score of swing strength using relative volume and candle dimensions. [Ref: Scoring-Logic]
        private decimal CalculateSignificanceScore(DomainCandle candle)
        {
            // Calculates absolute body size of candidate candle. [Ref: Body-Size]
            decimal body = Math.Abs((decimal)candle.Close.Value - (decimal)candle.Open.Value);
            // Calculates full shadow range printed by the exchange. [Ref: Full-Shadow-Range]
            decimal total = (decimal)candle.High.Value - (decimal)candle.Low.Value;

            // Guard against division by zero in flat liquidity periods. [Ref: Volume-Guard]
            if (total == 0) return 0m;

            // Measures structural quality based on body to range distribution. [Ref: Distribution-Ratio]
            decimal bodyRatio = body / total;
            // Retrieves trading volume recorded on exchange ticks. [Ref: Raw-Volume]
            decimal volume = (decimal)candle.Volume.Value;

            // Returns composite significance index representing price action pressure. [Ref: Score-Return]
            return bodyRatio * volume;
        }
    }
}