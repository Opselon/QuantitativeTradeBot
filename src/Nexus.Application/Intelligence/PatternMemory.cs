using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public class PatternMemory : IPatternMemory
    {
        private readonly ConcurrentBag<HistoricalPattern> _store = new();

        public int Count => _store.Count;

        public void Store(MarketVector vector, string conditions, string outcome, double performance)
        {
            _store.Add(new HistoricalPattern(vector, conditions, outcome, performance));
        }

        public IReadOnlyList<PatternMatchResult> Search(MarketVector queryVector, double similarityThreshold)
        {
            var results = new List<PatternMatchResult>();
            var queryArray = queryVector.ToFloatArray();

            foreach (var item in _store)
            {
                double sim = CalculateCosineSimilarity(queryArray, item.VectorArray);
                if (sim >= similarityThreshold)
                {
                    results.Add(new PatternMatchResult(item.Vector, item.Conditions, item.Outcome, item.Performance, sim));
                }
            }

            return results.OrderByDescending(r => r.Similarity).ToList();
        }

        private static double CalculateCosineSimilarity(float[] v1, float[] v2)
        {
            if (v1.Length != v2.Length || v1.Length == 0) return 0.0;

            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < v1.Length; i++)
            {
                dotProduct += v1[i] * v2[i];
                normA += v1[i] * v1[i];
                normB += v2[i] * v2[i];
            }

            if (normA == 0.0 || normB == 0.0) return 0.0;

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private class HistoricalPattern
        {
            public MarketVector Vector { get; }
            public float[] VectorArray { get; }
            public string Conditions { get; }
            public string Outcome { get; }
            public double Performance { get; }

            public HistoricalPattern(MarketVector vector, string conditions, string outcome, double performance)
            {
                Vector = vector;
                VectorArray = vector.ToFloatArray();
                Conditions = conditions ?? string.Empty;
                Outcome = outcome ?? string.Empty;
                Performance = performance;
            }
        }
    }
}
