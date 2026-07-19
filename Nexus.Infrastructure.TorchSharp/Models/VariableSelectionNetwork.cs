// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (Deep Learning Backend)
// FILE:    VariableSelectionNetwork.cs
// ============================================================================

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Nexus.Infrastructure.TorchSharp.Models
{
    /// <summary>
    /// Variable Selection Network (VSN) block of the Temporal Fusion Transformer.
    /// Dynamically evaluates and weights the most statistically significant features per market state.
    /// </summary>
    public class VariableSelectionNetwork : Module<Tensor, Tensor>
    {
        private readonly Module<Tensor, Tensor> _softmax;
        private readonly List<GatedResidualNetwork> _featureGrns = new();
        private readonly GatedResidualNetwork _selectorGrn;
        private readonly Module<Tensor, Tensor> _weightLinear;
        private readonly long _featureCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableSelectionNetwork"/> class.
        /// </summary>
        public VariableSelectionNetwork(long featureCount, long featureDim, long hiddenDim, double dropoutRate) : base("VariableSelectionNetwork")
        {
            _featureCount = featureCount;
            _softmax = Softmax(dim: -1);
            _weightLinear = Linear(featureCount * featureDim, featureCount);

            // FIXED: Selector GRN now maps and aligns input feature count with its own dimensions
            _selectorGrn = new GatedResidualNetwork(featureCount * featureDim, featureCount * featureDim, hiddenDim, dropoutRate);

            // Construct localized Gated Residual Networks for each independent input feature
            for (int i = 0; i < featureCount; i++)
            {
                // FIXED: Each feature's GRN now maps input dimension (featureDim = 1) to network representation dimension (hiddenDim = 64)
                var grn = new GatedResidualNetwork(featureDim, hiddenDim, hiddenDim, dropoutRate);
                _featureGrns.Add(grn);
                register_module($"feature_grn_{i}", grn);
            }

            register_module("selector_grn", _selectorGrn);
            register_module("weight_linear", _weightLinear);
            register_module("softmax", _softmax);
        }

        /// <summary>
        /// Forward propagation sequence. Assigns attention weights to variables.
        /// </summary>
        public override Tensor forward(Tensor input)
        {
            using (var scope = NewDisposeScope())
            {
                // Input size expected: [Batch, FeatureCount, FeatureDimension]
                var flattenedInput = input.view(input.shape[0], -1);

                // Step 1: Compute selection weights across all variable streams
                var selectionWeights = _softmax.forward(_weightLinear.forward(_selectorGrn.forward(flattenedInput)));

                // Step 2: Independently process and weight each feature slice
                var weightedFeaturesList = new List<Tensor>();
                for (int i = 0; i < _featureCount; i++)
                {
                    var featureSlice = input.select(dim: 1, index: i);
                    var transformedFeature = _featureGrns[i].forward(featureSlice);

                    var weight = selectionWeights.select(dim: 1, index: i).unsqueeze(-1);
                    weightedFeaturesList.Add(transformedFeature.mul(weight));
                }

                // Step 3: Re-aggregate and consolidate weighted outputs -> shape: [Batch, HiddenDim = 64]
                var stackedResult = stack(weightedFeaturesList, dim: 1);
                var aggregatedSum = stackedResult.sum(dim: 1);

                return aggregatedSum.MoveToOuterDisposeScope();
            }
        }
    }
}