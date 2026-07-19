// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (Deep Learning Backend)
// FILE:    GatedResidualNetwork.cs
// ============================================================================

using System;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Nexus.Infrastructure.TorchSharp.Models
{
    /// <summary>
    /// Gated Residual Network (GRN) block of the Temporal Fusion Transformer.
    /// Provides adaptive non-linear processing and context-aware gating to filter noise.
    /// Supports dimensional mapping projection via linear skip-connections if input and output dimensions differ.
    /// </summary>
    public class GatedResidualNetwork : Module<Tensor, Tensor>
    {
        private readonly Module<Tensor, Tensor> _linear1;
        private readonly Module<Tensor, Tensor> _linear2;
        private readonly Module<Tensor, Tensor> _gateLinear;
        private readonly Module<Tensor, Tensor> _layerNorm;
        private readonly Module<Tensor, Tensor> _activation;

        /// <summary>
        /// Optional projection layer to align skip-connection dimensions when inputDim != outputDim.
        /// </summary>
        private readonly Module<Tensor, Tensor>? _skipLinear;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatedResidualNetwork"/> class.
        /// </summary>
        public GatedResidualNetwork(long inputDim, long outputDim, long hiddenDim, double dropoutRate) : base("GatedResidualNetwork")
        {
            _linear1 = Linear(inputDim, hiddenDim);
            _linear2 = Linear(hiddenDim, outputDim);
            _gateLinear = Linear(inputDim, outputDim);
            _layerNorm = LayerNorm(new long[] { outputDim });
            _activation = ELU();

            // FIXED: If input and output dimensions differ (e.g. projecting 1 feature to 64 hidden dimensions inside VSN),
            // construct a linear skip-connection projection to align tensor addition boundaries.
            if (inputDim != outputDim)
            {
                _skipLinear = Linear(inputDim, outputDim);
                register_module("skipLinear", _skipLinear);
            }

            register_module("linear1", _linear1);
            register_module("linear2", _linear2);
            register_module("gateLinear", _gateLinear);
            register_module("layerNorm", _layerNorm);
            register_module("activation", _activation);
        }

        /// <summary>
        /// Forward propagation sequence. Computes gating residuals.
        /// </summary>
        public override Tensor forward(Tensor input)
        {
            using (var scope = NewDisposeScope())
            {
                // Step 1: Forward non-linear feedforward projection
                var h1 = _activation.forward(_linear1.forward(input));
                var h2 = _linear2.forward(h1);

                // Step 2: Calculate sigmoid gating weights (Gated Linear Unit equivalent)
                var gateSigmoid = sigmoid(_gateLinear.forward(input));
                var gatedOutput = h2.mul(gateSigmoid);

                // Step 3: Compute residual skip connection and apply normalization
                // FIXED: Direct projection of skip connection if dimensions are unaligned
                var skip = _skipLinear != null ? _skipLinear.forward(input) : input;
                var residualSum = add(skip, gatedOutput);
                var normalizedOutput = _layerNorm.forward(residualSum);

                return normalizedOutput.MoveToOuterDisposeScope();
            }
        }
    }
}