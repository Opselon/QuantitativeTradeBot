using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    public enum UncertaintyLevel
    {
        Low,
        Medium,
        High,
        Unknown
    }

    /// <summary>
    /// Service contract for evaluating uncertainty from market and neural states.
    /// </summary>
    public interface IUncertaintyEngine
    {
        UncertaintyLevel EvaluateUncertainty(MarketState marketState, double neuralConfidence, double consensusAgreement);
    }

    public sealed class UncertaintyEngine : IUncertaintyEngine
    {
        public UncertaintyLevel EvaluateUncertainty(MarketState marketState, double neuralConfidence, double consensusAgreement)
        {
            if (marketState == null)
            {
                return UncertaintyLevel.Unknown;
            }

            // High volatility + low neural confidence = High Uncertainty
            // Extreme sideways regime or high trend divergence = High Uncertainty
            double volatility = marketState.Volatility;

            if (volatility > 0.8 || neuralConfidence < 0.4 || consensusAgreement < 0.3)
            {
                return UncertaintyLevel.High;
            }

            if (volatility > 0.5 || neuralConfidence < 0.65 || consensusAgreement < 0.6)
            {
                return UncertaintyLevel.Medium;
            }

            if (volatility <= 0.5 && neuralConfidence >= 0.65 && consensusAgreement >= 0.6)
            {
                return UncertaintyLevel.Low;
            }

            return UncertaintyLevel.Unknown;
        }
    }
}
