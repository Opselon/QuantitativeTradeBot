using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Interfaces;
using Nexus.Infrastructure.TorchSharp.Models;
using TorchSharp;

namespace Nexus.Infrastructure.TorchSharp.Inference
{
    /// <summary>
    /// Backend-agnostic abstraction wrapping TorchSharp inference.
    /// This engine is thread-safe for concurrent read access during live market ticks.
    /// </summary>
    public class TorchInferenceEngine : IInferenceEngine, IDisposable
    {
        private MlpTradingModel? _activeModel;
        private string _activeModelId = string.Empty;
        private readonly torch.Device _device;
        private readonly object _lock = new();

        public TorchInferenceEngine()
        {
            // Automatically select GPU if CUDA is available, otherwise fallback to CPU
            _device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;
        }

        public Task LoadModelAsync(ModelMetadata metadata, CancellationToken ct = default)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            if (!File.Exists(metadata.CheckpointPath))
                throw new FileNotFoundException($"Model checkpoint not found at {metadata.CheckpointPath}");

            lock (_lock)
            {
                UnloadModel(); // Clean up previous tensor memory

                // Future: Use Factory to resolve architecture (e.g. LSTM, Transformer) based on metadata.ArchitectureType
                _activeModel = new MlpTradingModel(inputFeatures: 64, hiddenSize: 128);
                _activeModel.load(metadata.CheckpointPath);
                _activeModel.to(_device);
                _activeModel.eval(); // Set model to evaluation mode (disables dropout, batchnorm updates)

                _activeModelId = metadata.ModelId;
            }

            return Task.CompletedTask;
        }

        public Task<Prediction> PredictAsync(double[] normalizedFeatures, CancellationToken ct = default)
        {
            lock (_lock)
            {
                #region Graceful Neural Fallback Gate
                // REASON: If no neural model has been trained or promoted to Champion yet,
                // return a safe, neutral fallback prediction instead of throwing an InvalidOperationException.
                // This ensures the live trading pipeline operates smoothly on rule-based logics
                // and does not crash or log massive exceptions on startup.
                if (_activeModel == null)
                {
                    var fallbackProbabilities = new Dictionary<string, double>
                    {
                        { "Wait", 1.0 },
                        { "Buy", 0.0 },
                        { "Sell", 0.0 }
                    };

                    var fallbackPrediction = new Prediction(
                        ModelId: "AWAITING_INITIAL_TRAINING_RUN",
                        TargetSymbol: "UNKNOWN",
                        Probabilities: fallbackProbabilities,
                        ExpectedValue: 0.0,
                        Confidence: 1.0, // Fully confident in waiting
                        FeatureContributions: new Dictionary<string, double>(),
                        TimestampUtc: DateTime.UtcNow
                    );

                    return Task.FromResult(fallbackPrediction);
                }
                #endregion

                #region Active TorchSharp Model Inference Path
                // 1. Convert C# double[] to Torch Tensor
                using var inputTensor = torch.tensor(normalizedFeatures, dtype: torch.float32).unsqueeze(0).to(_device);

                // 2. Forward Pass (No Gradient tracking for performance)
                using var noGrad = torch.no_grad();
                var (policyLogits, expectedValueTensor) = _activeModel.forward(inputTensor);

                // 3. Apply Softmax to policy logits to get human-readable probabilities
                using var probabilitiesTensor = torch.nn.functional.softmax(policyLogits, dim: 1);

                float[] probs = probabilitiesTensor.data<float>().ToArray();
                float ev = expectedValueTensor.item<float>();

                // Output mapping: Index 0 = Wait, Index 1 = Buy, Index 2 = Sell
                var probabilityDict = new Dictionary<string, double>
                {
                    { "Wait", probs[0] },
                    { "Buy", probs[1] },
                    { "Sell", probs[2] }
                };

                // Calculate a simple confidence metric (Max probability)
                double confidence = Math.Max(probs[0], Math.Max(probs[1], probs[2]));

                var prediction = new Prediction(
                    ModelId: _activeModelId,
                    TargetSymbol: "UNKNOWN",
                    Probabilities: probabilityDict,
                    ExpectedValue: ev,
                    Confidence: confidence,
                    FeatureContributions: new Dictionary<string, double>(),
                    TimestampUtc: DateTime.UtcNow
                );

                return Task.FromResult(prediction);
                #endregion
            }
        }

        public void UnloadModel()
        {
            if (_activeModel != null)
            {
                _activeModel.Dispose();
                _activeModel = null;
                _activeModelId = string.Empty;
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            UnloadModel();
        }
    }
}