// Imports fundamental system types. [Ref: Core-Sys]
// Imports the Concurrent collections for multi-threaded cache handling. [Ref: Core-Concurrency]
// Imports threading cancellation components. [Ref: Core-Threading]
// Imports Task library for asynchronous pipelines. [Ref: Core-Tasks]
// Imports core abstractions. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
using System.Collections.Concurrent;
// Imports Compiler Services to bind states to objects at runtime. [Ref: Advanced-DotNet]
using System.Runtime.CompilerServices;
// Imports computed candle results models. [Ref: Proj-Dependency]
// Maps the Domain Candle entity to prevent naming conflicts with namespaces. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace dedicated to candle pattern engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.CandlePattern
{
    // Declares the EngulfingDetector which implements the PriceAction engine contract. [Ref: Engine-Def]
    public class EngulfingDetector : IPriceActionEngine
    {
        // Thread-safely maps pipeline contexts to their respective bullish engulfing detections. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> BullishEngulfingCache = new();

        // Thread-safely maps pipeline contexts to their respective bearish engulfing detections. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> BearishEngulfingCache = new();

        // Exposes a query helper to retrieve bullish engulfing state at any timestamp. [Ref: Public-API]
        public static bool IsBullishEngulfing(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached bullish pattern cache for the given context. [Ref: Cache-Lookup]
            if (BullishEngulfingCache.TryGetValue(context, out var dictionary))
            {
                // Returns the boolean result if found, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if context has no bullish engulfing registry. [Ref: Fallback]
            return false;
        }

        // Exposes a query helper to retrieve bearish engulfing state at any timestamp. [Ref: Public-API]
        public static bool IsBearishEngulfing(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached bearish pattern cache for the given context. [Ref: Cache-Lookup]
            if (BearishEngulfingCache.TryGetValue(context, out var dictionary))
            {
                // Returns the boolean result if found, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if context has no bearish engulfing registry. [Ref: Fallback]
            return false;
        }

        // Analyzes consecutive candles to detect and register Engulfing patterns. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context object is not null. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Gets or attaches a thread-safe dictionary for bullish engulfing. [Ref: Cache-Write]
            var bullishDict = BullishEngulfingCache.GetOrCreateValue(context);

            // Gets or attaches a thread-safe dictionary for bearish engulfing. [Ref: Cache-Write]
            var bearishDict = BearishEngulfingCache.GetOrCreateValue(context);

            // Exits immediately if we have fewer than 2 candles, as engulfing requires a pair. [Ref: Edge-Case]
            if (context.RawCandles.Count < 2)
            {
                // Returns the task with unmodified context. [Ref: Early-Exit]
                return Task.FromResult(context);
            }

            // Initializes the first candle (Index 0) to false because it has no historical precursor. [Ref: Boundary-Set]
            DateTime firstTimestamp = context.RawCandles[0].Timestamp;

            // Stores false for the first candle's bullish engulfing state. [Ref: State-Tag]
            bullishDict.TryAdd(firstTimestamp, false);

            // Stores false for the first candle's bearish engulfing state. [Ref: State-Tag]
            bearishDict.TryAdd(firstTimestamp, false);

            // Loops through the raw candles starting from index 1 to safely look backward. [Ref: Iteration]
            for (int i = 1; i < context.RawCandles.Count; i++)
            {
                // Aborts calculation if pipeline execution has been canceled. [Ref: Safe-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Holds the previous candle in the chronological list. [Ref: Read-State]
                DomainCandle prevCandle = context.RawCandles[i - 1];

                // Holds the current candle in the chronological list. [Ref: Read-State]
                DomainCandle currCandle = context.RawCandles[i];

                // Safely verifies both candles have pre-computed Phase 1 results in the context. [Ref: Validation]
                if (!context.CandleResults.TryGetValue(prevCandle.Timestamp, out var prevAnalysis) ||
                    !context.CandleResults.TryGetValue(currCandle.Timestamp, out var currAnalysis))
                {
                    // Skips current iteration if math records are missing. [Ref: Loop-Control]
                    continue;
                }

                // Extracts raw decimal boundary values for the previous candle. [Ref: Price-Extraction]
                decimal prevOpen = (decimal)prevCandle.Open.Value;

                // Extracts raw decimal boundary values for the previous candle. [Ref: Price-Extraction]
                decimal prevClose = (decimal)prevCandle.Close.Value;

                // Extracts raw decimal boundary values for the current candle. [Ref: Price-Extraction]
                decimal currOpen = (decimal)currCandle.Open.Value;

                // Extracts raw decimal boundary values for the current candle. [Ref: Price-Extraction]
                decimal currClose = (decimal)currCandle.Close.Value;

                // Declares the boolean flag for the bullish engulfing logic. [Ref: State-Declaration]
                bool isBullishEngulfing = false;

                // Declares the boolean flag for the bearish engulfing logic. [Ref: State-Declaration]
                bool isBearishEngulfing = false;

                // Check 1: Bullish Engulfing pattern rules. [Ref: Pattern-Rules]
                if (prevAnalysis.IsBearish && currAnalysis.IsBullish)
                {
                    // Verifies current body wraps around previous body and is larger. [Ref: Geometry-Logic]
                    bool engulfsBottom = currOpen <= prevClose;

                    // Verifies current body wraps around previous body and is larger. [Ref: Geometry-Logic]
                    bool engulfsTop = currClose >= prevOpen;

                    // Ensures the size of current candle body is strictly larger than previous. [Ref: Magnitude-Logic]
                    bool isLargerBody = currAnalysis.BodySize > prevAnalysis.BodySize;

                    // Evaluates absolute pattern trigger condition. [Ref: Boolean-Combine]
                    isBullishEngulfing = engulfsBottom && engulfsTop && isLargerBody;
                }
                // Check 2: Bearish Engulfing pattern rules. [Ref: Pattern-Rules]
                else if (prevAnalysis.IsBullish && currAnalysis.IsBearish)
                {
                    // Verifies current body wraps around previous body and is larger. [Ref: Geometry-Logic]
                    bool engulfsTop = currOpen >= prevClose;

                    // Verifies current body wraps around previous body and is larger. [Ref: Geometry-Logic]
                    bool engulfsBottom = currClose <= prevOpen;

                    // Ensures current body magnitude is strictly larger than previous. [Ref: Magnitude-Logic]
                    bool isLargerBody = currAnalysis.BodySize > prevAnalysis.BodySize;

                    // Evaluates absolute pattern trigger condition. [Ref: Boolean-Combine]
                    isBearishEngulfing = engulfsTop && engulfsBottom && isLargerBody;
                }

                // Atomically records the detection status for the current timestamp. [Ref: State-Persist]
                bullishDict.AddOrUpdate(currCandle.Timestamp, isBullishEngulfing, (key, oldVal) => isBullishEngulfing);

                // Atomically records the detection status for the current timestamp. [Ref: State-Persist]
                bearishDict.AddOrUpdate(currCandle.Timestamp, isBearishEngulfing, (key, oldVal) => isBearishEngulfing);
            }

            // Returns the context which has now been updated with engulfing flags. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}