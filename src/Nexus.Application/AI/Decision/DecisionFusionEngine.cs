using System;
using Nexus.Core.AI.Entities;
using Nexus.Core.Entities;
using Nexus.Core.Enums;

namespace Nexus.Application.AI.Decision
{
    /// <summary>
    /// The ultimate quantitative consensus boundary engine.
    /// Merges raw Neural Network probability distributions (Prediction) with deterministic multi-timeframe 
    /// trend metrics (ConsensusState) and strict risk guard boundaries (RiskState) to resolve high-frequency 
    /// market, limit, or stop orders.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Executed by: src/Nexus.Application/AI/Decision/AiTradingOrchestrator.cs
    /// - Output: src/Nexus.Core/Entities/TradeDecision.cs
    /// - Core Enums Used: src/Nexus.Core/Enums/DecisionAction.cs, src/Nexus.Core/Enums/RiskLevel.cs
    /// </remarks>
    public class DecisionFusionEngine
    {
        #region Core Decision Fusion Matrix
        /// <summary>
        /// Fuses mathematical neural network predictions with macro technical rules and capital exposure states.
        /// </summary>
        /// <param name="prediction">The live forward-pass predictions outputted by the TorchSharp models.</param>
        /// <param name="ruleConsensus">The multi-timeframe validated trend bias and overall market confidence.</param>
        /// <param name="riskState">The system's active equity drawdown metrics and security exposure limits.</param>
        /// <returns>A signed, volume-calibrated <see cref="TradeDecision"/> ready for direct broker execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any crucial execution snapshot is null.</exception>
        public TradeDecision Fuse(Prediction prediction, ConsensusState ruleConsensus, RiskState riskState)
        {
            #region Phase 1: Input Validation & Hard Risk Constraints
            if (prediction == null) throw new ArgumentNullException(nameof(prediction));
            if (ruleConsensus == null) throw new ArgumentNullException(nameof(ruleConsensus));
            if (riskState == null) throw new ArgumentNullException(nameof(riskState));

            // CRITICAL GUARD: Hard Risk constraints immediately override and veto all AI network signals.
            // Blocks execution if RiskLevel falls within the critical emergency tier (Extreme, Critical, TerminalState, LiquidationImminent).
            bool isRiskCritical = riskState.IsTradingBlocked ||
                                  riskState.RiskLevel == RiskLevel.Extreme ||
                                  riskState.RiskLevel == RiskLevel.Critical ||
                                  riskState.RiskLevel == RiskLevel.TerminalState ||
                                  riskState.RiskLevel == RiskLevel.LiquidationImminent;

            if (isRiskCritical)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    $"[VETO] Risk constraints active. Ingress blocked. Detailed Risk Level: {riskState.RiskLevel}",
                    DateTime.UtcNow);
            }
            #endregion

            #region Phase 2: AI Probability Evaluation
            // Extract the soft-max probability densities from the neural network classification layer
            double buyProb = prediction.Probabilities.GetValueOrDefault("Buy", 0.0);
            double sellProb = prediction.Probabilities.GetValueOrDefault("Sell", 0.0);
            double waitProb = prediction.Probabilities.GetValueOrDefault("Wait", 1.0);

            // Default fallback action is always WAIT (Safety First)
            DecisionAction baseAiDirection = DecisionAction.WAIT;
            double targetConfidence = waitProb;

            // Determine raw neural direction based on probability dominance and positive expected utility value (EV)
            if (buyProb > sellProb && buyProb > waitProb && prediction.ExpectedValue > 0.0)
            {
                baseAiDirection = DecisionAction.BUY;
                targetConfidence = buyProb;
            }
            else if (sellProb > buyProb && sellProb > waitProb && prediction.ExpectedValue < 0.0)
            {
                baseAiDirection = DecisionAction.SELL;
                targetConfidence = sellProb;
            }
            #endregion

            #region Phase 3: Macro-Trend Rule Filters
            // Macro-Trend Guard: Reject neural market executions if they run counter to dominant macro-trend biases.
            // Note: Limit orders are excluded from strict trend filters since they are designed for counter-trend mean reversion.
            if (baseAiDirection == DecisionAction.BUY && ruleConsensus.DominantBias == TrendDirection.BEARISH)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    "Fusion Rejected: AI Buy market signal conflicts with dominant Macro BEARISH trend bias.",
                    DateTime.UtcNow);
            }

            if (baseAiDirection == DecisionAction.SELL && ruleConsensus.DominantBias == TrendDirection.BULLISH)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    "Fusion Rejected: AI Sell market signal conflicts with dominant Macro BULLISH trend bias.",
                    DateTime.UtcNow);
            }
            #endregion

            #region Phase 4: Market Regime Adaptive Order Selection (Market vs. Limit vs. Stop)
            DecisionAction finalResolvedAction = DecisionAction.WAIT;
            string strategyClassificationReason = "Standby mode.";

            if (baseAiDirection == DecisionAction.BUY)
            {
                if (ruleConsensus.DominantBias == TrendDirection.NEUTRAL || ruleConsensus.BiasStrength < 0.45)
                {
                    // Scenario A: Neutral / Ranging Regime -> Execute Buy Limit below market structure to avoid spread premium
                    finalResolvedAction = DecisionAction.BUY_LIMIT;
                    strategyClassificationReason = "Mean Reversion Regime: Placed BUY_LIMIT at discount levels.";
                }
                else if (ruleConsensus.DominantBias == TrendDirection.BULLISH && ruleConsensus.BiasStrength > 0.80)
                {
                    // Scenario B: Extreme Volatility Breakout -> Execute Buy Stop above key resistance to confirm momentum breakout
                    finalResolvedAction = DecisionAction.BUY_STOP;
                    strategyClassificationReason = "Aggressive Breakout Regime: Placed BUY_STOP above market structure.";
                }
                else
                {
                    // Scenario C: Balanced Trend following -> Execute instant Market order
                    finalResolvedAction = DecisionAction.BUY;
                    strategyClassificationReason = "Balanced Trend following: Executed instant Market BUY.";
                }
            }
            else if (baseAiDirection == DecisionAction.SELL)
            {
                if (ruleConsensus.DominantBias == TrendDirection.NEUTRAL || ruleConsensus.BiasStrength < 0.45)
                {
                    // Scenario A: Neutral / Ranging Regime -> Execute Sell Limit above market structure to catch premium prices
                    finalResolvedAction = DecisionAction.SELL_LIMIT;
                    strategyClassificationReason = "Mean Reversion Regime: Placed SELL_LIMIT at premium levels.";
                }
                else if (ruleConsensus.DominantBias == TrendDirection.BEARISH && ruleConsensus.BiasStrength > 0.80)
                {
                    // Scenario B: Extreme Volatility Breakdown -> Execute Sell Stop below support structure to capture sell momentum
                    finalResolvedAction = DecisionAction.SELL_STOP;
                    strategyClassificationReason = "Aggressive Breakdown Regime: Placed SELL_STOP below market support.";
                }
                else
                {
                    // Scenario C: Balanced Trend following -> Execute instant Market Sell order
                    finalResolvedAction = DecisionAction.SELL;
                    strategyClassificationReason = "Balanced Trend following: Executed instant Market SELL.";
                }
            }
            #endregion

            #region Phase 5: Dynamic lot-sizing calculation based on 20-level High-Fidelity Risk Classification
            // Base safety lot size
            double calculatedVolume = 0.01;

            if (finalResolvedAction != DecisionAction.WAIT)
            {
                // Dynamic sizing maps perfectly to the 20-tier granularity
                if (targetConfidence > 0.80 &&
                    (riskState.RiskLevel == RiskLevel.RiskFree || riskState.RiskLevel == RiskLevel.UltraLow || riskState.RiskLevel == RiskLevel.Low))
                {
                    // Aggressive scale-in: High Neural Confidence & absolute low risk tier
                    calculatedVolume = 0.05;
                }
                else if (targetConfidence > 0.65 &&
                         (riskState.RiskLevel == RiskLevel.Normal || riskState.RiskLevel == RiskLevel.Medium || riskState.RiskLevel == RiskLevel.Moderate))
                {
                    // Standard scale-in inside the Balanced Operations tier
                    calculatedVolume = 0.02;
                }

                // Risk Mitigation override: Force minimum size if risk level escalates to the Aggressive/Elevated tier
                bool isRiskEscalated = riskState.RiskLevel == RiskLevel.Elevated ||
                                       riskState.RiskLevel == RiskLevel.High ||
                                       riskState.RiskLevel == RiskLevel.HighlyElevated ||
                                       riskState.RiskLevel == RiskLevel.Severe;

                if (isRiskEscalated)
                {
                    calculatedVolume = 0.01;
                }
            }
            #endregion

            #region Phase 6: Decision Signature
            string auditLogReason = $"[FUSION APPROVED] {strategyClassificationReason} | Model Conf: {targetConfidence:P1} | EV: {prediction.ExpectedValue:F2} | Output Lot: {calculatedVolume:F2}";

            return new TradeDecision(
                finalResolvedAction,
                calculatedVolume,
                auditLogReason,
                DateTime.UtcNow);
            #endregion
        }
        #endregion
    }
}