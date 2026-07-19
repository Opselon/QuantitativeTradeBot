using Nexus.Core.AI.Entities;
using Nexus.Core.Entities;
using Nexus.Core.Enums;

namespace Nexus.Application.AI.Decision
{
    /// <summary>
    /// The ultimate consensus boundary.
    /// Merges raw Neural Network outputs (Prediction) with deterministic hard-rules (ConsensusState)
    /// and strict capital constraints (RiskState) to produce a final, execution-ready TradeDecision.
    /// </summary>
    public class DecisionFusionEngine
    {
        public TradeDecision Fuse(Prediction prediction, ConsensusState ruleConsensus, RiskState riskState)
        {
            if (prediction == null) throw new ArgumentNullException(nameof(prediction));
            if (ruleConsensus == null) throw new ArgumentNullException(nameof(ruleConsensus));
            if (riskState == null) throw new ArgumentNullException(nameof(riskState));

            // 1. Hard Risk constraints override everything
            if (riskState.IsTradingBlocked || riskState.RiskLevel == RiskLevel.Extreme)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    $"Risk blocked. Level: {riskState.RiskLevel}",
                    DateTime.UtcNow);
            }

            // 2. Evaluate AI Probabilities
            double buyProb = prediction.Probabilities.GetValueOrDefault("Buy", 0);
            double sellProb = prediction.Probabilities.GetValueOrDefault("Sell", 0);
            double waitProb = prediction.Probabilities.GetValueOrDefault("Wait", 1.0);

            DecisionAction aiAction = DecisionAction.WAIT;
            double targetConfidence = waitProb;

            if (buyProb > sellProb && buyProb > waitProb && prediction.ExpectedValue > 0)
            {
                aiAction = DecisionAction.BUY;
                targetConfidence = buyProb;
            }
            else if (sellProb > buyProb && sellProb > waitProb && prediction.ExpectedValue < 0)
            {
                aiAction = DecisionAction.SELL;
                targetConfidence = sellProb;
            }

            // 3. Fusion Logic (AI vs Rule Engine)
            // Example: AI must agree with the Dominant Macro Bias to enter.
            if (aiAction == DecisionAction.BUY && ruleConsensus.DominantBias == TrendDirection.BEARISH)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    "Fusion Rejected: AI Buy signal conflicts with Macro Bearish rule consensus.",
                    DateTime.UtcNow);
            }

            if (aiAction == DecisionAction.SELL && ruleConsensus.DominantBias == TrendDirection.BULLISH)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    "Fusion Rejected: AI Sell signal conflicts with Macro Bullish rule consensus.",
                    DateTime.UtcNow);
            }

            // 4. Position Sizing based on Confidence and Risk
            double baseVolume = 0.01;
            if (targetConfidence > 0.8 && riskState.RiskLevel == RiskLevel.Low)
            {
                baseVolume = 0.05; // Scale up on high confidence & low risk
            }

            return new TradeDecision(
                aiAction,
                baseVolume,
                $"Fusion Approved. Model {prediction.ModelId} Conf: {targetConfidence:P1}. EV: {prediction.ExpectedValue:F2}",
                DateTime.UtcNow);
        }
    }
}