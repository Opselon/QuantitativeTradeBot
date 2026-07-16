using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.MarketIntelligence.Features;

namespace Nexus.MarketIntelligence.Memory
{
    /// <summary>
    /// Thread-safe in-memory adapter implementing <see cref="IMarketStateMemory"/> for local evaluation.
    /// Uses cosine similarity to find closest matching historical feature vectors.
    /// </summary>
    public sealed class LocalStateMemory : IMarketStateMemory
    {
        private readonly ConcurrentBag<MemoryEntry> _storage = new ConcurrentBag<MemoryEntry>();

        /// <inheritdoc />
        public Task StoreStateAsync(MarketState state, ExtractedFeatures features, string outcome = "Pending", CancellationToken ct = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (features == null) throw new ArgumentNullException(nameof(features));

            _storage.Add(new MemoryEntry(state, features, outcome));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<HistoricalMatch>> FindSimilarStatesAsync(string symbol, ExtractedFeatures currentFeatures, double similarityThreshold, CancellationToken ct = default)
        {
            if (currentFeatures == null) throw new ArgumentNullException(nameof(currentFeatures));

            var results = new List<HistoricalMatch>();
            double[] targetArr = currentFeatures.ToDoubleArray();

            foreach (var entry in _storage)
            {
                if (ct.IsCancellationRequested) break;

                if (!string.Equals(entry.State.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
                    continue;

                double[] candidateArr = entry.Features.ToDoubleArray();
                if (candidateArr.Length != targetArr.Length)
                    continue; // Skip mismatching vector sizes

                double sim = CalculateCosineSimilarity(targetArr, candidateArr);
                if (sim >= similarityThreshold)
                {
                    results.Add(new HistoricalMatch(entry.State, sim, entry.Outcome));
                }
            }

            // Order by closest matches first
            IReadOnlyList<HistoricalMatch> sortedMatches = results.OrderByDescending(m => m.Similarity).ToList();
            return Task.FromResult(sortedMatches);
        }

        #region Cosine Similarity Math

        private static double CalculateCosineSimilarity(double[] vectorA, double[] vectorB)
        {
            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA <= 0.0 || normB <= 0.0) return 0.0;

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        #endregion

        #region Internal Helper Struct

        private sealed class MemoryEntry
        {
            public MarketState State { get; }
            public ExtractedFeatures Features { get; }
            public string Outcome { get; }

            public MemoryEntry(MarketState state, ExtractedFeatures features, string outcome)
            {
                State = state;
                Features = features;
                Outcome = outcome;
            }
        }

        #endregion
    }
}
