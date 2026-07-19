using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Nexus.Infrastructure.TorchSharp.Models
{
    /// <summary>
    /// Multi-Layer Perceptron (MLP) MVP Implementation for Nexus Quantitative Trading.
    /// Maps continuous market features to [Wait, Buy, Sell] probabilities and an Expected Value.
    /// </summary>
    public sealed class MlpTradingModel : Module<Tensor, (Tensor Probabilities, Tensor ExpectedValue)>
    {
        private readonly Module<Tensor, Tensor> _sharedNetwork;
        private readonly Module<Tensor, Tensor> _policyHead; // Output: [Wait, Buy, Sell] logits
        private readonly Module<Tensor, Tensor> _valueHead;  // Output: Expected movement (EV)

        public MlpTradingModel(int inputFeatures = 64, int hiddenSize = 128) : base("MlpTradingModel")
        {
            // Shared feature extraction layers
            _sharedNetwork = Sequential(
                Linear(inputFeatures, hiddenSize),
                ReLU(),
                Dropout(0.2),
                Linear(hiddenSize, hiddenSize / 2),
                ReLU()
            );

            // Classification head for Action probabilities (Wait=0, Buy=1, Sell=2)
            _policyHead = Linear(hiddenSize / 2, 3);

            // Regression head for Expected Value (Pip prediction)
            _valueHead = Linear(hiddenSize / 2, 1);

            RegisterComponents();
        }

        public override (Tensor Probabilities, Tensor ExpectedValue) forward(Tensor input)
        {
            var features = _sharedNetwork.forward(input);

            var policyLogits = _policyHead.forward(features);
            var expectedValue = _valueHead.forward(features);

            // Note: We return raw logits for the policy head because CrossEntropyLoss expects them.
            // Softmax should only be applied during Inference, not Training.
            return (policyLogits, expectedValue);
        }
    }
}