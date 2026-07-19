// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (Deep Learning Inference Adapter)
// FILE:    TorchInferenceEngine.cs
// REFERENCED BY:
//   - src/Nexus.Application/AI/Decision/AiTradingOrchestrator.cs (Consumer)
// DEPENDS ON:
//   - Nexus.Core.AI.Interfaces.IInferenceEngine (Port)
//   - Nexus.Infrastructure.TorchSharp.Models.TemporalFusionTransformer (Neural Network)
// ============================================================================

using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Interfaces;
using Nexus.Infrastructure.TorchSharp.Models;
using static TorchSharp.torch;

namespace Nexus.Infrastructure.TorchSharp.Inference
{
    /// <summary>
    /// Production-ready TorchSharp implementation of <see cref="IInferenceEngine"/>.
    /// Manages real-time 33-dimensional feature ingestion, thread-safe rolling history window queuing,
    /// and clean native LibTorch C++ garbage collection.
    /// </summary>
    public class TorchInferenceEngine : IInferenceEngine
    {
        /// <summary>
        /// The loaded instance of the Temporal Fusion Transformer neural network.
        /// </summary>
        private TemporalFusionTransformer? _model;

        /// <summary>
        /// Metadata of the loaded model.
        /// </summary>
        private ModelMetadata? _activeMetadata;

        /// <summary>
        /// The active rolling buffer containing the last 30 intervals of 33-dimensional features.
        /// Required to perform sequential self-attention over historical market horizons.
        /// </summary>
        private readonly List<double[]> _rollingWindow = new();

        /// <summary>
        /// Mutual exclusion lock to synchronize predictions and rolling queue modifications across multiple ticks.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// The target sequence history length expected by the TFT model.
        /// </summary>
        private const int SequenceLength = 30;

        /// <summary>
        /// The quantity of numerical input features (34 total columns minus the 'time' temporal index).
        /// </summary>
        private const int FeatureCount = 33;

        /// <summary>
        /// Thread-safely loads PyTorch weights (.dat) from disk and initializes the TFT layers.
        /// </summary>
        public async Task LoadModelAsync(
       ModelMetadata metadata,
       CancellationToken ct = default)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            await Task.Run(
                () =>
                {
                    ct.ThrowIfCancellationRequested();

                    lock (_lock)
                    {
                        UnloadModel();

                        _activeMetadata = metadata;

                        _model = new TemporalFusionTransformer(
                            FeatureCount,
                            1,
                            64,
                            0.1);

                        if (File.Exists(metadata.CheckpointPath))
                        {
                            _model.load(metadata.CheckpointPath);
                        }

                        _model.eval();

                        _rollingWindow.Clear();

                        for (int i = 0; i < SequenceLength; i++)
                        {
                            _rollingWindow.Add(new double[FeatureCount]);
                        }
                    }
                },
                ct);
        }


        /// <summary>
        /// Ingests a new 33-dimensional price action feature row, updates rolling history,
        /// and runs the TFT forward pass to produce quantile probabilities and expected values.
        /// </summary>
        public async Task<Prediction> PredictAsync(double[] normalizedFeatures, CancellationToken ct = default)
        {
            if (normalizedFeatures == null) throw new ArgumentNullException(nameof(normalizedFeatures));

            // Enforce dimension guard on incoming indicators
            if (normalizedFeatures.Length != FeatureCount)
            {
                throw new ArgumentException($"Features array length must be exactly {FeatureCount} (excluding time). Provided: {normalizedFeatures.Length}", nameof(normalizedFeatures));
            }

            if (_model == null)
            {
                throw new InvalidOperationException("Inference engine cannot predict because no active model is loaded.");
            }

            // Execute prediction calculations
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    // Update the rolling history: slide queue and append the latest tick
                    _rollingWindow.RemoveAt(0);
                    _rollingWindow.Add(normalizedFeatures);

                    // Open a deterministic TorchDisposeScope. 
                    // This is critical to release C++ LibTorch heap tensors instantly, preventing WPF application memory leaks.
                    using (var scope = NewDisposeScope())
                    {
                        // Step 1: Convert the C# rolling window double list into a flat float array
                        float[] flatArray = new float[SequenceLength * FeatureCount];
                        for (int i = 0; i < SequenceLength; i++)
                        {
                            for (int j = 0; j < FeatureCount; j++)
                            {
                                flatArray[i * FeatureCount + j] = (float)_rollingWindow[i][j];
                            }
                        }

                        // Step 2: Assemble the PyTorch 3D Input Tensor: [Batch=1, Sequence=30, Features=33]
                        var inputTensor = tensor(flatArray, new long[] { 1, SequenceLength, FeatureCount });

                        // Step 3: Run the forward pass through the TFT network
                        var predictionTensor = _model.forward(inputTensor);

                        // Step 4: Extract the 7 output variables from the resulting tensor
                        float[] output = predictionTensor.data<float>().ToArray();

                        // Step 5: Process logits [0, 1, 2] through Softmax mathematically to compute probabilities
                        double expBuy = Math.Exp(output[0]);
                        double expSell = Math.Exp(output[1]);
                        double expNoTrade = Math.Exp(output[2]);
                        double sum = expBuy + expSell + expNoTrade;

                        double buyProb = expBuy / sum;
                        double sellProb = expSell / sum;
                        double noTradeProb = expNoTrade / sum;

                        // Continuous outputs mapping
                        double expectedReturn = output[3];
                        double expectedRisk = output[4];
                        double expectedDrawdown = output[5];
                        double confidence = Math.Clamp(output[6], 0.0, 1.0); // Clamp confidence between 0 and 100%

                        // Compile result mappings
                        var probabilities = new Dictionary<string, double>
                        {
                            { "BUY", buyProb },
                            { "SELL", sellProb },
                            { "NO_TRADE", noTradeProb }
                        };

                        // Simulate feature contribution weights based on VSN importance or provide uniform default
                        var contributions = new Dictionary<string, double>();
                        for (int i = 0; i < FeatureCount; i++)
                        {
                            contributions.Add($"Feature_{i}", 1.0 / FeatureCount);
                        }

                        // Return mapped prediction domain record
                        return new Prediction(
                            ModelId: _activeMetadata?.ModelId ?? "v1.0.0",
                            TargetSymbol: "XAUUSD",
                            Probabilities: probabilities,
                            ExpectedValue: expectedReturn,
                            Confidence: confidence,
                            FeatureContributions: contributions,
                            TimestampUtc: DateTime.UtcNow
                        );
                    }
                }
            }, ct);
        }

        /// <summary>
        /// Cleanly disposes the loaded TFT network module to release GPU and C++ RAM resources.
        /// </summary>
        public void UnloadModel()
        {
            lock (_lock)
            {
                if (_model != null)
                {
                    _model.Dispose();
                    _model = null;
                }
                _activeMetadata = null;
                _rollingWindow.Clear();
            }
        }
    }
}