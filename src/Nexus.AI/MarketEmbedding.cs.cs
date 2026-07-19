// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   CORE LAYER (Domain Entities)
// FILE:    MarketEmbedding.cs
// REFERENCED BY:
//   - src/Nexus.Core/AI/Interfaces/IExperienceMemory.cs
//   - src/Nexus.Infrastructure.TorchSharp/Models/TemporalFusionTransformer.cs
// ============================================================================

using System;

namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Represents an immutable dense 64-element numerical vector embedding of the market state.
    /// Captures the complete consolidated context of momentum, liquidity, and volatility regimes.
    /// Designed specifically to map raw charts into the high-dimensional latent space of the TFT model.
    /// </summary>
    public sealed class MarketEmbedding
    {
        /// <summary>
        /// The physical dense 64-element array backing field.
        /// </summary>
        private readonly float[] _vector;

        /// <summary>
        /// The precise UTC timestamp when this embedding was computed.
        /// </summary>
        private readonly DateTime _timestampUtc;

        /// <summary>
        /// The target financial asset symbol (e.g. XAUUSD, EURUSD).
        /// </summary>
        private readonly string _symbol;

        /// <summary>
        /// Gets the immutable dense 64-element float array feature vector.
        /// </summary>
        public float[] Vector => _vector;

        /// <summary>
        /// Gets the exact UTC timestamp of context generation.
        /// </summary>
        public DateTime TimestampUtc => _timestampUtc;

        /// <summary>
        /// Gets the asset symbol identifier.
        /// </summary>
        public string Symbol => _symbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketEmbedding"/> class.
        /// </summary>
        /// <param name="symbol">The target trading symbol.</param>
        /// <param name="vector">The dense 64-dimensional float array.</param>
        /// <param name="timestampUtc">The UTC timestamp.</param>
        /// <exception cref="ArgumentNullException">Thrown if symbol or vector is null.</exception>
        /// <exception cref="ArgumentException">Thrown if vector length is not exactly 64.</exception>
        public MarketEmbedding(string symbol, float[] vector, DateTime timestampUtc)
        {
            _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            _vector = vector ?? throw new ArgumentNullException(nameof(vector));

            // Enforce vector dimension alignment constraint as per TFT input specifications
            if (vector.Length != 64)
            {
                throw new ArgumentException("The market embedding vector must be exactly 64-dimensional.", nameof(vector));
            }

            _timestampUtc = timestampUtc;
        }
    }
}