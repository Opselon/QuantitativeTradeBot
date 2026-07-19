using Nexus.Core.AI.Interfaces;
using Nexus.Core.Entities;

namespace Nexus.Application.AI.Features
{
    public class FeatureOrchestrator
    {
        private readonly IReadOnlyList<IFeatureExtractor> _extractors;
        public const int TargetDimension = 64;

        public FeatureOrchestrator(IEnumerable<IFeatureExtractor> extractors)
        {
            _extractors = new List<IFeatureExtractor>(extractors);
        }

        /// <summary>
        /// Compiles the output of all registered feature extractors into a single, contiguous tensor array.
        /// </summary>
        public double[] GenerateFeatureVector(MarketState state, IReadOnlyList<Candle> candles, IReadOnlyList<Tick> ticks)
        {
            var vector = new double[TargetDimension];
            int currentIndex = 0;

            foreach (var extractor in _extractors)
            {
                var partialFeatures = extractor.Extract(state, candles, ticks);

                // Prevent overflowing the target tensor size
                int copyLength = Math.Min(partialFeatures.Length, TargetDimension - currentIndex);
                if (copyLength <= 0) break;

                Array.Copy(partialFeatures, 0, vector, currentIndex, copyLength);
                currentIndex += copyLength;
            }

            // Pad the remainder with zeros if the extractors did not fill all 64 slots
            while (currentIndex < TargetDimension)
            {
                vector[currentIndex++] = 0.0;
            }

            return vector;
        }

        public string GetFeatureSetVersionHash()
        {
            // Simple hash string combining all extractor names and versions to uniquely identify the dataset schema
            return string.Join("|", _extractors.Select(e => $"{e.Name}_v{e.Version}")).GetHashCode().ToString("X");
        }
    }
}