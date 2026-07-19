// Imports essential system types and tools. [Ref: Core-Sys]
// Imports thread-safe collections for caching patterns. [Ref: Core-Concurrency]
// Imports threading cancellation abstractions. [Ref: Core-Threading]
// Imports asynchronous Task primitives. [Ref: Core-Tasks]
// Imports pipeline abstractions. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
using System.Collections.Concurrent;
// Imports compiler runtime services to bind patterns dynamically to objects. [Ref: Advanced-DotNet]
using System.Runtime.CompilerServices;
// Maps the Domain Candle entity to prevent naming conflicts with namespaces. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace dedicated to candle pattern engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.CandlePattern
{
    // Declares the InsideBarDetector class implementing the IPriceActionEngine contract. [Ref: Engine-Def]
    public class InsideBarDetector : IPriceActionEngine
    {
        // Thread-safely attaches a pattern storage dictionary to the processed pipeline context. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> InsideBarCache = new();

        // Exposes a public helper to query the detected Inside Bar state at any given timestamp. [Ref: Public-API]
        public static bool IsInsideBar(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached inside bar dictionary dynamically from the context. [Ref: Cache-Lookup]
            if (InsideBarCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed pattern state if it exists, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cached dictionary is present. [Ref: Fallback]
            return false;
        }

        // Detects and registers Inside Bar patterns asynchronously in the pipeline. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context object is not null. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Gets or attaches a thread-safe dictionary for inside bar patterns. [Ref: Cache-Write]
            var insideBarDict = InsideBarCache.GetOrCreateValue(context);

            // Checks if the candle stream contains fewer than two items and exits early if so. [Ref: Edge-Case]
            if (context.RawCandles.Count < 2)
            {
                // Returns the task holding the unmodified context reference. [Ref: Early-Exit]
                return Task.FromResult(context);
            }

            // Fetches the timestamp of the first candle in the data series. [Ref: Boundary-Set]
            DateTime firstTimestamp = context.RawCandles[0].Timestamp;

            // Inserts false for the first candle's inside bar state as there is no previous bar. [Ref: State-Tag]
            insideBarDict.TryAdd(firstTimestamp, false);

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

                // Verifies if current High is capped inside the previous High. [Ref: Geometry-Logic]
                bool isHighContained = currHigh <= prevHigh;

                // Verifies if current Low is protected inside the previous Low. [Ref: Geometry-Logic]
                bool isLowContained = currLow >= prevLow;

                // Combines logic; candle is an Inside Bar if both high and low are strictly enclosed. [Ref: Pattern-Logic]
                bool isInsideBar = isHighContained && isLowContained;

                // Atomically registers the detection state in the attached dictionary. [Ref: State-Persist]
                insideBarDict.AddOrUpdate(currCandle.Timestamp, isInsideBar, (key, oldVal) => isInsideBar);
            }

            // Returns the context instance containing attached inside bar records. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}