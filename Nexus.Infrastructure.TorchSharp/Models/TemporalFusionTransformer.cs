// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (Deep Learning Backend)
// FILE:    TemporalFusionTransformer.cs
// ============================================================================

using static global::TorchSharp.torch;
using static global::TorchSharp.torch.nn;
using MultiheadAttentionModule =
    global::TorchSharp.Modules.MultiheadAttention;
using Tensor = global::TorchSharp.torch.Tensor;

namespace Nexus.Infrastructure.TorchSharp.Models
{
    /// <summary>
    /// Temporal Fusion Transformer for sequential market-data processing.
    /// </summary>
    public sealed class TemporalFusionTransformer :
        Module<Tensor, Tensor>
    {
        private const long AttentionHeadCount = 4;
        private const long OutputDimension = 7;

        private readonly long _featureCount;
        private readonly long _featureDimension;
        private readonly long _hiddenDimension;

        private readonly VariableSelectionNetwork _variableSelectionNetwork;
        private readonly GatedResidualNetwork _preAttentionNetwork;
        private readonly GatedResidualNetwork _postAttentionNetwork;
        private readonly MultiheadAttentionModule _multiheadAttention;
        private readonly Module<Tensor, Tensor> _outputProjection;

        /// <summary>
        /// Creates a Temporal Fusion Transformer instance.
        /// </summary>
        public TemporalFusionTransformer(
            long featureCount,
            long featureDimension,
            long hiddenDimension,
            double dropoutRate)
            : base(nameof(TemporalFusionTransformer))
        {
            ValidateConstructorArguments(
                featureCount,
                featureDimension,
                hiddenDimension,
                dropoutRate);

            _featureCount = featureCount;
            _featureDimension = featureDimension;
            _hiddenDimension = hiddenDimension;

            _variableSelectionNetwork = new VariableSelectionNetwork(
                featureCount,
                featureDimension,
                hiddenDimension,
                dropoutRate);

            _preAttentionNetwork = new GatedResidualNetwork(
                hiddenDimension,
                hiddenDimension,
                hiddenDimension,
                dropoutRate);

            _postAttentionNetwork = new GatedResidualNetwork(
                hiddenDimension,
                hiddenDimension,
                hiddenDimension,
                dropoutRate);

            _multiheadAttention = MultiheadAttention(
                hiddenDimension,
                AttentionHeadCount,
                dropoutRate);

            _outputProjection = Linear(
                hiddenDimension,
                OutputDimension);

            register_module(
                "variableSelectionNetwork",
                _variableSelectionNetwork);

            register_module(
                "preAttentionNetwork",
                _preAttentionNetwork);

            register_module(
                "postAttentionNetwork",
                _postAttentionNetwork);

            register_module(
                "multiheadAttention",
                _multiheadAttention);

            register_module(
                "outputProjection",
                _outputProjection);
        }

        /// <summary>
        /// Executes forward propagation.
        /// Input:  [batch, sequenceLength, featureCount]
        /// Output: [batch, 7]
        /// </summary>
        public override Tensor forward(Tensor input)
        {
            if (input is null)
            {
                throw new global::System.ArgumentNullException(nameof(input));
            }

            ValidateForwardInput(input);

            using (var scope = NewDisposeScope())
            {
                long batchSize = input.shape[0];
                long sequenceLength = input.shape[1];

                // Scalar market features normally use featureDimension = 1.
                // Input:
                // [batch, sequenceLength, featureCount]
                //
                // VSN input:
                // [batch * sequenceLength, featureCount, featureDimension]
                Tensor variableSelectionInput = input.reshape(
                    batchSize * sequenceLength,
                    _featureCount,
                    _featureDimension);

                // [batch * sequenceLength, hiddenDimension]
                Tensor selectedFeatures =
                    _variableSelectionNetwork.forward(
                        variableSelectionInput);

                // [batch, sequenceLength, hiddenDimension]
                Tensor sequenceRepresentation = selectedFeatures.reshape(
                    batchSize,
                    sequenceLength,
                    _hiddenDimension);

                // [batch, sequenceLength, hiddenDimension]
                Tensor preAttentionContext =
                    _preAttentionNetwork.forward(
                        sequenceRepresentation);

                // MultiheadAttention in this TorchSharp version expects:
                // [sequenceLength, batch, hiddenDimension]
                Tensor attentionInput =
                    preAttentionContext.transpose(0, 1);

                var attentionResult = _multiheadAttention.forward(
                    attentionInput,
                    attentionInput,
                    attentionInput,
                    null,
                    false,
                    null);

                // Item1:
                // [sequenceLength, batch, hiddenDimension]
                Tensor attentionSequence = attentionResult.Item1;

                // [batch, sequenceLength, hiddenDimension]
                Tensor attentionOutput =
                    attentionSequence.transpose(0, 1);

                // Select latest time step:
                // [batch, hiddenDimension]
                Tensor latestContext = attentionOutput.select(
                    dim: 1,
                    index: sequenceLength - 1);

                // [batch, hiddenDimension]
                Tensor postAttentionContext =
                    _postAttentionNetwork.forward(
                        latestContext);

                // [batch, 7]
                Tensor prediction =
                    _outputProjection.forward(
                        postAttentionContext);

                return prediction.MoveToOuterDisposeScope();
            }
        }

        /// <summary>
        /// Calculates exponentially weighted element-wise MSE.
        /// </summary>
        /// <param name="prediction">
        /// Prediction with shape [batch, 7].
        /// </param>
        /// <param name="target">
        /// Target with shape [batch, 7].
        /// </param>
        /// <param name="timeOffsets">
        /// Time offsets with shape [batch] or [batch, 1].
        /// </param>
        /// <param name="lambda">
        /// Non-negative exponential-decay coefficient.
        /// </param>
        /// <returns>A scalar loss tensor.</returns>
        public Tensor CalculateExponentialDecayLoss(
            Tensor prediction,
            Tensor target,
            Tensor timeOffsets,
            double lambda)
        {
            if (prediction is null)
            {
                throw new global::System.ArgumentNullException(
                    nameof(prediction));
            }

            if (target is null)
            {
                throw new global::System.ArgumentNullException(
                    nameof(target));
            }

            if (timeOffsets is null)
            {
                throw new global::System.ArgumentNullException(
                    nameof(timeOffsets));
            }

            ValidateLossArguments(
                prediction,
                target,
                timeOffsets,
                lambda);

            using (var scope = NewDisposeScope())
            {
                long batchSize = prediction.shape[0];

                // Critical broadcasting fix:
                // [batch] -> [batch, 1]
                Tensor normalizedOffsets = timeOffsets.reshape(
                    batchSize,
                    1);

                // Element-wise MSE:
                // [batch, 7]
                Tensor elementLoss =
                    global::TorchSharp.torch.nn.functional.mse_loss(
                        prediction,
                        target,
                        Reduction.None);

                // [batch, 1]
                Tensor decayWeights = exp(
                    normalizedOffsets.mul(-lambda));

                // [batch, 7] * [batch, 1] => [batch, 7]
                Tensor weightedLoss =
                    elementLoss.mul(decayWeights);

                // Scalar loss.
                Tensor finalLoss =
                    weightedLoss.mean();

                return finalLoss.MoveToOuterDisposeScope();
            }
        }

        private void ValidateForwardInput(Tensor input)
        {
            if (input.shape.Length != 3)
            {
                throw new global::System.ArgumentException(
                    "TFT input must have shape " +
                    "[batch, sequenceLength, featureCount].");
            }

            if (input.shape[0] <= 0)
            {
                throw new global::System.ArgumentException(
                    "TFT input batch size must be greater than zero.");
            }

            if (input.shape[1] <= 0)
            {
                throw new global::System.ArgumentException(
                    "TFT sequence length must be greater than zero.");
            }

            if (input.shape[2] != _featureCount)
            {
                throw new global::System.ArgumentException(
                    "TFT expected " +
                    _featureCount +
                    " features, but received " +
                    input.shape[2] +
                    ".");
            }
        }

        private static void ValidateLossArguments(
            Tensor prediction,
            Tensor target,
            Tensor timeOffsets,
            double lambda)
        {
            if (double.IsNaN(lambda) ||
                double.IsInfinity(lambda) ||
                lambda < 0.0)
            {
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(lambda));
            }

            if (prediction.shape.Length != 2)
            {
                throw new global::System.ArgumentException(
                    "Prediction must have shape [batch, outputDimension].");
            }

            if (target.shape.Length != 2)
            {
                throw new global::System.ArgumentException(
                    "Target must have shape [batch, outputDimension].");
            }

            if (prediction.shape[0] != target.shape[0] ||
                prediction.shape[1] != target.shape[1])
            {
                throw new global::System.ArgumentException(
                    "Prediction and target must have identical shapes.");
            }

            if (prediction.shape[1] != OutputDimension)
            {
                throw new global::System.ArgumentException(
                    "Prediction output dimension must be " +
                    OutputDimension +
                    ".");
            }

            if (timeOffsets.shape.Length != 1 &&
                timeOffsets.shape.Length != 2)
            {
                throw new global::System.ArgumentException(
                    "Time offsets must have shape [batch] or [batch, 1].");
            }

            if (timeOffsets.shape[0] != prediction.shape[0])
            {
                throw new global::System.ArgumentException(
                    "Time-offset batch size must match prediction batch size.");
            }

            if (timeOffsets.shape.Length == 2 &&
                timeOffsets.shape[1] != 1)
            {
                throw new global::System.ArgumentException(
                    "Two-dimensional offsets must have shape [batch, 1].");
            }
        }

        private static void ValidateConstructorArguments(
            long featureCount,
            long featureDimension,
            long hiddenDimension,
            double dropoutRate)
        {
            if (featureCount <= 0)
            {
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(featureCount));
            }

            if (featureDimension <= 0)
            {
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(featureDimension));
            }

            if (hiddenDimension <= 0)
            {
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(hiddenDimension));
            }

            if (hiddenDimension % AttentionHeadCount != 0)
            {
                throw new global::System.ArgumentException(
                    "Hidden dimension must be divisible by " +
                    AttentionHeadCount +
                    ".");
            }

            if (double.IsNaN(dropoutRate) ||
                double.IsInfinity(dropoutRate) ||
                dropoutRate < 0.0 ||
                dropoutRate >= 1.0)
            {
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(dropoutRate));
            }
        }
    }
}
