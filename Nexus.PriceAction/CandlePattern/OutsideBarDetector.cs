// Imports fundamental types and utility structures. [Ref: Core-Sys]
// Imports concurrent structures for safe thread-safe caching. [Ref: Core-Concurrency]
// Imports cancellation thread controls. [Ref: Core-Threading]
// Imports async task abstractions. [Ref: Core-Tasks]
// Imports general price action engine pipeline context. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
using System.Collections.Concurrent;
// Imports compiler operations to attach values to objects dynamically. [Ref: Advanced-DotNet]
using System.Runtime.CompilerServices;
// Maps the Domain Candle entity to prevent naming conflicts with namespaces. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace dedicated to candle pattern engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.CandlePattern
{
    // Declares the OutsideBarDetector class implementing the IPriceActionEngine contract. [Ref: Engine-Def]
    public class OutsideBarDetector : IPriceActionEngine
    {
        // Thread-safely attaches an outside bar pattern storage dictionary to the processed context. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> OutsideBarCache = new();

        // Exposes a public helper to query the detected Outside Bar state at any given timestamp. [Ref: Public-API]
        public static bool IsOutsideBar(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached outside bar dictionary dynamically from the context. [Ref: Cache-Lookup]
            if (OutsideBarCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed pattern state if it exists, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cached dictionary is present. [Ref: Fallback]
            return false;
        }

        // Detects and registers Outside Bar patterns asynchronously in the pipeline. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context object is not null. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Gets or attaches a thread-safe dictionary for outside bar patterns. [Ref: Cache-Write]
            var outsideBarDict = OutsideBarCache.GetOrCreateValue(context);

            // Checks if the candle stream contains fewer than two items and exits early if so. [Ref: Edge-Case]
            if (context.RawCandles.Count < 2)
            {
                // Returns the task holding the unmodified context reference. [Ref: Early-Exit]
                return Task.FromResult(context);
            }

            // Fetches the timestamp of the first candle in the data series. [Ref: Boundary-Set]
            DateTime firstTimestamp = context.RawCandles[0].Timestamp;

            // Inserts false for the first candle's outside bar state as there is no previous bar. [Ref: State-Tag]
            outsideBarDict.TryAdd(firstTimestamp, false);

            // Iterates chronologically over raw candles starting from index 1 to look backward. [Ref: Iteration]
            for (int i = 1; i < context.RawCandles.Count; i++)
            {
                // Gracefully aborts iteration and throws if a cancellation request has been fired. [Ref: Safe-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Reads the previous candle in the chronological list. [Ref: Read-State]
                DomainCandle prevCandle = context.RawCandles[i - 1];

                // Reads the current candle in the chronological list. [Ref: Read-State]
                DomainCandle currCandle = context.RawCandles[i];

                // Extracts raw decimal high-bound price of the previous candle. [Ref: Price-Extraction]
                decimal prevHigh = (decimal)prevCandle.High.Value;

                // Extracts raw decimal low-bound price of the previous candle. [Ref: Price-Extraction]
                decimal prevLow = (decimal)prevCandle.Low.Value;

                // Extracts raw decimal high-bound price of the current candle. [Ref: Price-Extraction]
                decimal currHigh = (decimal)currCandle.High.Value;

                // Extracts raw decimal low-bound price of the current candle. [Ref: Price-Extraction]
                decimal currLow = (decimal)currCandle.Low.Value;

                // Verifies if current High has exceeded previous High (making a higher high). [Ref: Geometry-Logic]
                bool hasHigherHigh = currHigh > prevHigh;

                // Verifies if current Low has breached below previous Low (making a lower low). [Ref: Geometry-Logic]
                bool hasLowerLow = currLow < prevLow;

                // Combines logic; candle is an Outside Bar if it breaks out in both directions. [Ref: Pattern-Logic]
                bool isOutsideBar = hasHigherHigh && hasLowerLow;

                // Atomically registers the detection state in the attached dictionary. [Ref: State-Persist]
                outsideBarDict.AddOrUpdate(currCandle.Timestamp, isOutsideBar, (key, oldVal) => isOutsideBar);
            }

            // Returns the context instance containing attached outside bar records. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}