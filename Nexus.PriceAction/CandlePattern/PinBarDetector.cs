// Imports the System namespace for base exceptions and types. [Ref: Core-Sys]
// Imports the Concurrent namespace for thread-safe dictionaries. [Ref: Core-Concurrency]
// Imports Task-based asynchronous programming types. [Ref: Core-Tasks]
// Imports Task helper utilities. [Ref: Core-Tasks-Helper]
// Imports core abstractions and pipeline contexts. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
// Imports the candle analysis model computed in Phase 1. [Ref: Proj-Dependency]
using Nexus.PriceAction.Candle.Models;
using System.Collections.Concurrent;
// Imports Runtime Compiler Services for attaching dynamic states to existing objects. [Ref: Advanced-DotNet]
using System.Runtime.CompilerServices;

// Defines the namespace dedicated to candle pattern engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.CandlePattern
{
    // Declares the PinBarDetector implementing the pipeline orchestrator contract. [Ref: Engine-Def]
    public class PinBarDetector : IPriceActionEngine
    {
        // Thread-safely maps pipeline contexts to their respective bullish pinbar detections. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> BullishPinBarCache = new();

        // Thread-safely maps pipeline contexts to their respective bearish pinbar detections. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> BearishPinBarCache = new();

        // Exposes a public query method to read bullish pinbar status for any candle in the context. [Ref: Public-API]
        public static bool IsBullishPinBar(PriceActionContext context, DateTime timestamp)
        {
            // Tries to retrieve the bullish cache dictionary attached to the provided context. [Ref: Cache-Lookup]
            if (BullishPinBarCache.TryGetValue(context, out var dictionary))
            {
                // Returns the boolean detection value if it exists, otherwise defaults to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cache exists for the context. [Ref: Fallback]
            return false;
        }

        // Exposes a public query method to read bearish pinbar status for any candle in the context. [Ref: Public-API]
        public static bool IsBearishPinBar(PriceActionContext context, DateTime timestamp)
        {
            // Tries to retrieve the bearish cache dictionary attached to the provided context. [Ref: Cache-Lookup]
            if (BearishPinBarCache.TryGetValue(context, out var dictionary))
            {
                // Returns the boolean detection value if it exists, otherwise defaults to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cache exists for the context. [Ref: Fallback]
            return false;
        }

        // Analyzes the context to detect and tag pinbar patterns asynchronously. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Prevents null reference operations by strictly validating the incoming context. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Safely gets or creates the bullish dictionary attached to the lifetime of the context. [Ref: Cache-Write]
            var bullishDict = BullishPinBarCache.GetOrCreateValue(context);

            // Safely gets or creates the bearish dictionary attached to the lifetime of the context. [Ref: Cache-Write]
            var bearishDict = BearishPinBarCache.GetOrCreateValue(context);

            // Iterates over all analyzed candle results stored inside the context from Phase 1. [Ref: Iteration]
            foreach (var record in context.CandleResults)
            {
                // Aborts the loop and safely propagates the cancellation request if triggered. [Ref: Safe-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Holds the timestamp of the candle being processed. [Ref: Read-State]
                DateTime timestamp = record.Key;

                // Holds the computed mathematical analysis of the current candle. [Ref: Read-State]
                CandleAnalysisResult analysis = record.Value;

                // Extracts the total price range of the candle. [Ref: Read-State]
                decimal totalRange = analysis.TotalRange;

                // Bypasses calculations for frozen or invalid candles to prevent mathematical errors. [Ref: Guard-Clause]
                if (totalRange == 0)
                {
                    // Marks the bullish detection as false for the frozen candle. [Ref: State-Tag]
                    bullishDict.TryAdd(timestamp, false);

                    // Marks the bearish detection as false for the frozen candle. [Ref: State-Tag]
                    bearishDict.TryAdd(timestamp, false);

                    // Skips the remaining logic for this candle iteration. [Ref: Loop-Control]
                    continue;
                }

                // Checks if the body represents less than or equal to 30% of the candle's total height. [Ref: Formula-Rule]
                bool isSmallBody = analysis.BodyToRangeRatio <= 0.30m;

                // Verifies if the lower shadow is long, taking up at least 60% of the total range. [Ref: Formula-Rule]
                bool hasLongLowerShadow = analysis.LowerShadowSize >= (totalRange * 0.60m);

                // Verifies if the upper shadow is very small, representing 15% or less of the total range. [Ref: Formula-Rule]
                bool hasShortUpperShadow = analysis.UpperShadowSize <= (totalRange * 0.15m);

                // Verifies if the upper shadow is long, taking up at least 60% of the total range. [Ref: Formula-Rule]
                bool hasLongUpperShadow = analysis.UpperShadowSize >= (totalRange * 0.60m);

                // Verifies if the lower shadow is very small, representing 15% or less of the total range. [Ref: Formula-Rule]
                bool hasShortLowerShadow = analysis.LowerShadowSize <= (totalRange * 0.15m);

                // Evaluates if the candle structurally qualifies as a Bullish Pinbar (Hammer). [Ref: Pattern-Logic]
                bool isBullishPin = isSmallBody && hasLongLowerShadow && hasShortUpperShadow;

                // Evaluates if the candle structurally qualifies as a Bearish Pinbar (Shooting Star). [Ref: Pattern-Logic]
                bool isBearishPin = isSmallBody && hasLongUpperShadow && hasShortLowerShadow;

                // Atomically registers the bullish pinbar status inside the attached dictionary. [Ref: State-Persist]
                bullishDict.AddOrUpdate(timestamp, isBullishPin, (key, oldVal) => isBullishPin);

                // Atomically registers the bearish pinbar status inside the attached dictionary. [Ref: State-Persist]
                bearishDict.AddOrUpdate(timestamp, isBearishPin, (key, oldVal) => isBearishPin);
            }

            // Returns the unmodified context reference which now holds dynamically attached patterns. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}