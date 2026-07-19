// Imports essential system utilities and types. [Ref: Core-Sys]
using System;
// Imports thread-safe collections for storing pattern states. [Ref: Core-Concurrency]
using System.Collections.Concurrent;
// Imports runtime services to dynamically attach properties to instances. [Ref: Advanced-DotNet]
using System.Runtime.CompilerServices;
// Imports thread cancellation features. [Ref: Core-Threading]
using System.Threading;
// Imports task parallelism types. [Ref: Core-Tasks]
using System.Threading.Tasks;
// Imports core context abstractions. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
// Imports computed candle result models from Phase 1. [Ref: Proj-Dependency]
using Nexus.PriceAction.Candle.Enums;
// Imports computed candle result models from Phase 1. [Ref: Proj-Dependency]
using Nexus.PriceAction.Candle.Models;

// Defines the namespace dedicated to candle pattern engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.CandlePattern
{
    // Declares the DojiDetector class which implements the pipeline engine contract. [Ref: Engine-Def]
    public class DojiDetector : IPriceActionEngine
    {
        // Thread-safely attaches a doji pattern storage dictionary to the processed context. [Ref: Dynamic-Attach]
        private static readonly ConditionalWeakTable<PriceActionContext, ConcurrentDictionary<DateTime, bool>> DojiCache = new();

        // Exposes a public helper to query the detected Doji state at any given timestamp. [Ref: Public-API]
        public static bool IsDoji(PriceActionContext context, DateTime timestamp)
        {
            // Tries to look up the attached doji dictionary dynamically from the context. [Ref: Cache-Lookup]
            if (DojiCache.TryGetValue(context, out var dictionary))
            {
                // Returns the computed pattern state if it exists, defaulting to false. [Ref: Cache-Lookup]
                return dictionary.TryGetValue(timestamp, out var detected) && detected;
            }

            // Returns false if no cached dictionary is present. [Ref: Fallback]
            return false;
        }

        // Detects and registers Doji patterns asynchronously in the pipeline. [Ref: Pipeline-Execute]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context object is not null. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Gets or attaches a thread-safe dictionary for doji patterns. [Ref: Cache-Write]
            var dojiDict = DojiCache.GetOrCreateValue(context);

            // Iterates over all analyzed candle results stored inside the context from Phase 1. [Ref: Iteration]
            foreach (var record in context.CandleResults)
            {
                // Gracefully aborts iteration and throws if a cancellation request has been fired. [Ref: Safe-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Holds the timestamp of the candle being processed. [Ref: Read-State]
                DateTime timestamp = record.Key;

                // Holds the computed mathematical analysis of the current candle. [Ref: Read-State]
                CandleAnalysisResult analysis = record.Value;

                // Checks if the pre-computed type is Doji or if the body represents 5% or less of range. [Ref: Pattern-Logic]
                bool isDojiClassified = analysis.Type == CandleType.Doji;

                // Direct fallback calculation: ensures the body is tiny relative to total range. [Ref: Formula-Rule]
                bool isTinyBody = analysis.TotalRange > 0m && analysis.BodyToRangeRatio <= 0.05m;

                // Combines pre-computed type classification and fallback ratio rules. [Ref: Logic-Combine]
                bool isDoji = isDojiClassified || isTinyBody;

                // Atomically registers the detection state in the attached dictionary. [Ref: State-Persist]
                dojiDict.AddOrUpdate(timestamp, isDoji, (key, oldVal) => isDoji);
            }

            // Returns the context instance containing attached doji records. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}