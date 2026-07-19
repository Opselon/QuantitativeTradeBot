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
    // Declares the MorningStarDetector class implementing the IPriceActionEngine contract. [Ref: Engine-Def]
    public class MorningStarDetector : IPriceActionEngine
    {
        // Thread-safely attaches a morning star pattern storage dictionary to the processed context. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> MorningStarCache = new();

        // Exposes a public helper to query the detected Morning Star state at any given timestamp. [Ref: Public-API]
        public static bool IsMorningStar(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached morning star dictionary dynamically from the context. [Ref: Cache-Lookup]
            if (MorningStarCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed pattern state if it exists, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cached dictionary is present. [Ref: Fallback]
            return false;
        }

        // Detects and registers Morning Star patterns asynchronously in the pipeline. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context object is not null. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Gets or attaches a thread-safe dictionary for morning star patterns. [Ref: Cache-Write]
            var morningStarDict = MorningStarCache.GetOrCreateValue(context);

            // Checks if the candle stream contains fewer than three items and exits early if so. [Ref: Edge-Case]
            if (context.RawCandles.Count < 3)
            {
                // Returns the task holding the unmodified context reference. [Ref: Early-Exit]
                return Task.FromResult(context);
            }

            // Sets false for the first candle's morning star state as it has no history. [Ref: Boundary-Set]
            morningStarDict.TryAdd(context.RawCandles[0].Timestamp, false);

            // Sets false for the second candle's morning star state as it lacks a second historical bar. [Ref: Boundary-Set]
            morningStarDict.TryAdd(context.RawCandles[1].Timestamp, false);

            // Iterates chronologically over raw candles starting from index 2 to look back at three bars. [Ref: Iteration]
            for (int i = 2; i < context.RawCandles.Count; i++)
            {
                // Gracefully aborts iteration and throws if a cancellation request has been fired. [Ref: Safe-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Holds the first candle in the pattern (large bearish candle). [Ref: Read-State]
                DomainCandle prev2Candle = context.RawCandles[i - 2];

                // Holds the second candle in the pattern (small indecision star at the bottom). [Ref: Read-State]
                DomainCandle prev1Candle = context.RawCandles[i - 1];

                // Holds the third candle in the pattern (large bullish reversing candle). [Ref: Read-State]
                DomainCandle currCandle = context.RawCandles[i];

                // Safely verifies all three candles have pre-computed Phase 1 results in the context. [Ref: Validation]
                if (!context.CandleResults.TryGetValue(prev2Candle.Timestamp, out var prev2Analysis) ||
                    !context.CandleResults.TryGetValue(prev1Candle.Timestamp, out var prev1Analysis) ||
                    !context.CandleResults.TryGetValue(currCandle.Timestamp, out var currAnalysis))
                {
                    // Skips current iteration if math records are missing. [Ref: Loop-Control]
                    continue;
                }

                // Extracts raw decimal boundary values for the first candle. [Ref: Price-Extraction]
                decimal prev2Open = (decimal)prev2Candle.Open.Value;

                // Extracts raw decimal boundary values for the first candle. [Ref: Price-Extraction]
                decimal prev2Close = (decimal)prev2Candle.Close.Value;

                // Extracts raw decimal boundary values for the second candle. [Ref: Price-Extraction]
                decimal prev1Close = (decimal)prev1Candle.Close.Value;

                // Extracts raw decimal boundary values for the third candle. [Ref: Price-Extraction]
                decimal currClose = (decimal)currCandle.Close.Value;

                // Rule 1: The first candle (index i-2) must be strongly bearish. [Ref: Pattern-Rules]
                bool isFirstBearish = prev2Analysis.IsBearish && prev2Analysis.BodyToRangeRatio >= 0.50m;

                // Rule 2: The second candle (index i-1) must be a small body (indecision) near the bottom. [Ref: Pattern-Rules]
                bool isSecondIndecision = prev1Analysis.BodyToRangeRatio <= 0.30m;

                // Rule 2 Gap/Bottom Alignment: Second candle close must be lower or equal to the first close. [Ref: Pattern-Rules]
                bool isSecondAtBottom = prev1Close <= prev2Close;

                // Rule 3: The third candle (index i) must be strongly bullish. [Ref: Pattern-Rules]
                bool isThirdBullish = currAnalysis.IsBullish && currAnalysis.BodyToRangeRatio >= 0.40m;

                // Rule 3 Midpoint piercing: Bullish candle must close above the 50% midpoint of the first body. [Ref: Pattern-Rules]
                decimal firstMidpoint = prev2Close + (prev2Analysis.BodySize / 2m);

                // Verifies if the third candle's close successfully pierced the midpoint of the first. [Ref: Geometry-Logic]
                bool isMidpointPierced = currClose >= firstMidpoint;

                // Combines all structural and geometrical logic to evaluate the Morning Star. [Ref: Pattern-Logic]
                bool isMorningStar = isFirstBearish && isSecondIndecision && isSecondAtBottom && isThirdBullish && isMidpointPierced;

                // Atomically registers the detection state in the attached dictionary. [Ref: State-Persist]
                morningStarDict.AddOrUpdate(currCandle.Timestamp, isMorningStar, (key, oldVal) => isMorningStar);
            }

            // Returns the context instance containing attached morning star records. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}
