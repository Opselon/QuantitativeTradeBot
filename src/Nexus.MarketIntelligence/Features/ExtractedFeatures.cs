namespace Nexus.MarketIntelligence.Features
{
    /// <summary>
    /// Represents the deterministic set of features extracted from a market snapshot.
    /// Ready for downstream AI/ML model inference without downstream preprocessing.
    /// </summary>
    public sealed class ExtractedFeatures
    {
        /// <summary>
        /// Gets the UTC timestamp when features were extracted.
        /// </summary>
        public DateTime ExtractionTimestampUtc { get; }

        /// <summary>
        /// Gets the symbol for which features were extracted.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Gets the key-value dictionary of all extracted features.
        /// </summary>
        public IReadOnlyDictionary<string, double> Features { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ExtractedFeatures"/>.
        /// </summary>
        public ExtractedFeatures(string symbol, DateTime extractionTimestampUtc, IReadOnlyDictionary<string, double> features)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            ExtractionTimestampUtc = extractionTimestampUtc;
            Features = features ?? throw new ArgumentNullException(nameof(features));
        }

        /// <summary>
        /// Flattens the feature set into a float array for neural network input (e.g. ONNX).
        /// Order is guaranteed to be stable and deterministic.
        /// </summary>
        public float[] ToFloatArray()
        {
            var keys = new List<string>(Features.Keys);
            keys.Sort(StringComparer.Ordinal);

            float[] arr = new float[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                arr[i] = (float)Features[keys[i]];
            }
            return arr;
        }

        /// <summary>
        /// Flattens the feature set into a double array.
        /// Order is guaranteed to be stable and deterministic.
        /// </summary>
        public double[] ToDoubleArray()
        {
            var keys = new List<string>(Features.Keys);
            keys.Sort(StringComparer.Ordinal);

            double[] arr = new double[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                arr[i] = Features[keys[i]];
            }
            return arr;
        }
    }
}
