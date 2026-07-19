// Imports base system types. [Ref: Core-Sys]
using System;
// Imports thread-safe collections for storing pattern states. [Ref: Core-Concurrency]
using System.Collections.Concurrent;
// Imports compiler runtime services to bind patterns dynamically to objects. [Ref: Advanced-DotNet]
using System.Runtime.CompilerServices;
// Imports thread cancellation features. [Ref: Core-Threading]
using System.Threading;
// Imports task parallelism types. [Ref: Core-Tasks]
using System.Threading.Tasks;
// Imports core context abstractions. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
// Maps the Domain Candle entity to prevent naming conflicts with namespaces. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace dedicated to candle pattern engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.CandlePattern
{
    // Declares the ThreeWhiteSoldiersDetector class implementing the IPriceActionEngine contract. [Ref: Engine-Def]
    public class ThreeWhiteSoldiersDetector : IPriceActionEngine
    {
        // Thread-safely attaches a pattern storage dictionary to the processed context. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> ThreeWhiteSoldiersCache = new();

        // Exposes a public helper to query the detected Three White Soldiers state at any given timestamp. [Ref: Public-API]
        public static bool IsThreeWhiteSoldiers(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached pattern dictionary dynamically from the context. [Ref: Cache-Lookup]
            if (ThreeWhiteSoldiersCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed pattern state if it exists, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cached dictionary is present. [Ref: Fallback]
            return false;
        }

        // Detects and registers Three White Soldiers patterns asynchronously in the pipeline. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context object is not null. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Gets or attaches a thread-safe dictionary for Three White Soldiers patterns. [Ref: Cache-Write]
            var soldiersDict = ThreeWhiteSoldiersCache.GetOrCreateValue(context);

            // Checks if the candle stream contains fewer than three items and exits early if so. [Ref: Edge-Case]
            if (context.RawCandles.Count < 3)
            {
                // Returns the task holding the unmodified context reference. [Ref: Early-Exit]
                return Task.FromResult(context);
            }

            // Sets false for the first candle's pattern state as it has no history. [Ref: Boundary-Set]
            soldiersDict.TryAdd(context.RawCandles[0].Timestamp, false);

            // Sets false for the second candle's pattern state as it lacks enough historical bars. [Ref: Boundary-Set]
            soldiersDict.TryAdd(context.RawCandles[1].Timestamp, false);

            // Iterates chronologically over raw candles starting from index 2 to look back at three bars. [Ref: Iteration]
            for (int i = 2; i < context.RawCandles.Count; i++)
            {
                // Gracefully aborts iteration and throws if a cancellation request has been fired. [Ref: Safe-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Holds the first candle in the series. [Ref: Read-State]
                DomainCandle prev2Candle = context.RawCandles[i - 2];

                // Holds the second candle in the series. [Ref: Read-State]
                DomainCandle prev1Candle = context.RawCandles[i - 1];

                // Holds the third candle in the series. [Ref: Read-State]
                DomainCandle currCandle = context.RawCandles[i];

                // Safely verifies all three candles have pre-computed Phase 1 results in the context. [Ref: Validation]
                if (!context.CandleResults.TryGetValue(prev2Candle.Timestamp, out var prev2Analysis) ||
                    !context.CandleResults.TryGetValue(prev1Candle.Timestamp, out var prev1Analysis) ||
                    !context.CandleResults.TryGetValue(currCandle.Timestamp, out var currAnalysis))
                {
                    // Skips current iteration if math records are missing. [Ref: Loop-Control]
                    continue;
                }

                // Extracts raw decimal boundaries for the first candle. [Ref: Price-Extraction]
                decimal prev2Open = (decimal)prev2Candle.Open.Value;

                // Extracts raw decimal boundaries for the first candle. [Ref: Price-Extraction]
                decimal prev2Close = (decimal)prev2Candle.Close.Value;

                // Extracts raw decimal boundaries for the second candle. [Ref: Price-Extraction]
                decimal prev1Open = (decimal)prev1Candle.Open.Value;

                // Extracts raw decimal boundaries for the second candle. [Ref: Price-Extraction]
                decimal prev1Close = (decimal)prev1Candle.Close.Value;

                // Extracts raw decimal boundaries for the third candle. [Ref: Price-Extraction]
                decimal currOpen = (decimal)currCandle.Open.Value;

                // Extracts raw decimal boundaries for the third candle. [Ref: Price-Extraction]
                decimal currClose = (decimal)currCandle.Close.Value;

                // Rule 1: All three candles must be strongly bullish. [Ref: Pattern-Rules]
                bool allBullish = prev2Analysis.IsBullish && prev1Analysis.IsBullish && currAnalysis.IsBullish;

                // Rule 2: All three candles must have strong bodies relative to range (60% or more body). [Ref: Pattern-Rules]
                bool strongBodies = prev2Analysis.BodyToRangeRatio >= 0.60m &&
                                    prev1Analysis.BodyToRangeRatio >= 0.60m &&
                                    currAnalysis.BodyToRangeRatio >= 0.60m;

                // Rule 3: Sequential ascending close progression (making continuous higher closes). [Ref: Pattern-Rules]
                bool ascendingCloses = currClose > prev1Close && prev1Close > prev2Close;

                // Rule 4: Candle 2 open must occur inside Candle 1's real body. [Ref: Pattern-Rules]
                bool secondOpenInFirstBody = prev1Open > prev2Open && prev1Open < prev2Close;

                // Rule 5: Candle 3 open must occur inside Candle 2's real body. [Ref: Pattern-Rules]
                bool thirdOpenInSecondBody = currOpen > prev1Open && currOpen < prev1Close;

                // Combines all structural, momentum, and body overlapping checks. [Ref: Pattern-Logic]
                bool isThreeSoldiers = allBullish && strongBodies && ascendingCloses && secondOpenInFirstBody && thirdOpenInSecondBody;

                // Atomically registers the detection state in the attached dictionary. [Ref: State-Persist]
                soldiersDict.AddOrUpdate(currCandle.Timestamp, isThreeSoldiers, (key, oldVal) => isThreeSoldiers);
            }

            // Returns the context instance containing attached pattern records. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}