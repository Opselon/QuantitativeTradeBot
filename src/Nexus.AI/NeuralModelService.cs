using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.AI
{
    public class NeuralModelService : INeuralModelService, IDisposable
    {
        private InferenceSession? _session;
        private string _modelName = "None";
        private string _modelVersion = "0.0.0";
        private bool _isLoaded;
        private double _inferenceLatencyMs;
        private DateTime _lastExecutionTime = DateTime.MinValue;
        private ModelMode _currentMode = ModelMode.FALLBACK_MODE;

        public string CurrentModelName => _modelName;
        public string ModelVersion => _modelVersion;
        public bool IsLoaded => _isLoaded;
        public double InferenceLatencyMs => _inferenceLatencyMs;
        public DateTime LastExecutionTime => _lastExecutionTime;
        public ModelMode CurrentMode => _currentMode;

        public Task<bool> LoadModelAsync(string modelPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
            {
                _modelName = "None";
                _modelVersion = "0.0.0";
                _isLoaded = false;
                _currentMode = ModelMode.FALLBACK_MODE;
                return Task.FromResult(false);
            }

            try
            {
                _session?.Dispose();
                _session = new InferenceSession(modelPath);

                _modelName = Path.GetFileName(modelPath);

                // Extract version from ONNX metadata if available
                if (_session.ModelMetadata != null)
                {
                    _modelVersion = _session.ModelMetadata.Version.ToString();
                    if (_session.ModelMetadata.CustomMetadataMap != null &&
                        _session.ModelMetadata.CustomMetadataMap.TryGetValue("version", out string? customVer))
                    {
                        _modelVersion = customVer ?? _modelVersion;
                    }
                }
                else
                {
                    _modelVersion = "1.0.0-Loaded";
                }

                _isLoaded = true;
                _currentMode = ModelMode.ONNX_MODEL;
                return Task.FromResult(true);
            }
            catch (Exception)
            {
                _modelName = "None";
                _modelVersion = "0.0.0";
                _isLoaded = false;
                _currentMode = ModelMode.FALLBACK_MODE;
                return Task.FromResult(false);
            }
        }

        public Task<EvaluationResult> EvaluateAsync(MarketVector vector, CancellationToken cancellationToken = default)
        {
            _lastExecutionTime = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            if (!_isLoaded || _session == null || _currentMode == ModelMode.FALLBACK_MODE)
            {
                throw new InvalidOperationException("No deployed ONNX model is loaded; synthetic inference is prohibited.");
            }

            try
            {
                var inputName = _session.InputMetadata.Keys.FirstOrDefault() ?? "input";
                var inputMeta = _session.InputMetadata[inputName];

                // Standard shape: [1, 10]
                int[] dimensions = inputMeta.Dimensions.Length == 2 ? inputMeta.Dimensions : new[] { 1, 10 };
                // Ensure dimensions are positive
                for (int i = 0; i < dimensions.Length; i++)
                {
                    if (dimensions[i] < 0) dimensions[i] = i == 0 ? 1 : 10;
                }

                var floatVals = vector.ToFloatArray();
                var tensor = new DenseTensor<float>(floatVals, dimensions);

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, tensor)
                };

                using var outputs = _session.Run(inputs);

                // Parse outputs (assume logits or probabilities are returned)
                double buy = 0.4;
                double sell = 0.3;
                double wait = 0.3;
                double expectedMovement = 0.0;
                double riskScore = 0.5;

                var firstOutput = outputs.FirstOrDefault();
                if (firstOutput != null)
                {
                    var tensorData = firstOutput.AsTensor<float>();
                    if (tensorData != null && tensorData.Length >= 3)
                    {
                        var data = tensorData.ToArray();
                        buy = Softmax(data, 0);
                        sell = Softmax(data, 1);
                        wait = Softmax(data, 2);
                        if (data.Length >= 4) expectedMovement = data[3];
                        if (data.Length >= 5) riskScore = Math.Clamp(data[4], 0.0, 1.0);
                    }
                }

                double confidence = Math.Max(buy, Math.Max(sell, wait));
                string regime = vector.MarketRegime > 0.5 ? "Trend Bullish" : (vector.MarketRegime < -0.5 ? "Trend Bearish" : "Ranging / Volatile");

                var evaluation = new EvaluationResult(buy, sell, wait, expectedMovement, riskScore, confidence, regime);
                sw.Stop();
                _inferenceLatencyMs = sw.Elapsed.TotalMilliseconds;
                return Task.FromResult(evaluation);
            }
            catch
            {
                _isLoaded = false;
                _currentMode = ModelMode.FALLBACK_MODE;
                throw;
            }
        }

        private static double Softmax(float[] values, int index)
        {
            if (values == null || values.Length < 3) return 0.33;
            double sum = values.Take(3).Sum(v => Math.Exp(v));
            return Math.Exp(values[index]) / sum;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
