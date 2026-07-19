using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public class ScenarioEvaluationEngine : IScenarioEvaluationEngine
    {
        public ScenarioScore EvaluateScenarios(MarketState state)
        {
            if (state == null)
            {
                return new ScenarioScore(0.25, 0.25, 0.25, 0.25);
            }

            // High-fidelity scenario mapping based on real MarketState metrics
            double trendContinuation = Math.Clamp(0.5 + (state.Momentum * 0.4), 0.0, 1.0);
            double reversal = Math.Clamp(0.5 - (state.Momentum * 0.4), 0.0, 1.0);
            double liquidityFailure = Math.Clamp(0.1 + (1.0 - state.Liquidity) * 0.8, 0.0, 1.0);
            double volatilityExpansion = Math.Clamp(0.2 + (state.Volatility * 0.7), 0.0, 1.0);

            // Normalize
            double sum = trendContinuation + reversal + liquidityFailure + volatilityExpansion;
            if (sum > 0)
            {
                trendContinuation /= sum;
                reversal /= sum;
                liquidityFailure /= sum;
                volatilityExpansion /= sum;
            }

            return new ScenarioScore(trendContinuation, reversal, liquidityFailure, volatilityExpansion);
        }
    }
}
