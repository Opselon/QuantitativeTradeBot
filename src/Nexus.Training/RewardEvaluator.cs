using Nexus.Core.Entities;

namespace Nexus.Training
{
    /// <summary>
    /// Holds the detailed components of a quantitative reward score calculation.
    /// Provides full auditability for why a decision was given a specific learning reward.
    /// </summary>
    public sealed class RewardBreakdown
    {
        public double TotalReward { get; set; }
        public double ProfitComponent { get; set; }
        public double RiskAdjustedComponent { get; set; }
        public double DrawdownPenalty { get; set; }
        public double PredictionAccuracyComponent { get; set; }
        public double TimingComponent { get; set; }
        public double OvertradingPenalty { get; set; }
        public double UncertaintyPenalty { get; set; }
        public double RiskManagementPenalty { get; set; }

        public override string ToString()
        {
            return $"Total: {TotalReward:F2} [Profit: {ProfitComponent:F2}, Risk-Adjusted: {RiskAdjustedComponent:F2}, DD Penalty: {DrawdownPenalty:F2}, Prediction: {PredictionAccuracyComponent:F2}, Timing: {TimingComponent:F2}, Overtrading: {OvertradingPenalty:F2}, Uncertainty: {UncertaintyPenalty:F2}, Risk Management: {RiskManagementPenalty:F2}]";
        }
    }

    /// <summary>
    /// Evaluates the quantitative learning reward of a finished decision experience.
    /// Prioritizes decision quality and risk-adjusted performance over raw profit.
    /// </summary>
    public sealed class RewardEvaluator
    {
        private const double BaseScale = 10000.0; // Pips conversion scale (e.g. for EURUSD)

        /// <summary>
        /// Computes a multi-faceted score for a finalized experience sample.
        /// </summary>
        public RewardBreakdown Evaluate(ExperienceSample sample)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));

            var breakdown = new RewardBreakdown();

            // 1. PROFIT COMPONENT (Positive or Negative)
            // Profit is the physical result of the trade.
            double tradeProfitLoss = sample.Result; // This is the registered result (e.g., P&L or realized returns)
            breakdown.ProfitComponent = tradeProfitLoss * 5.0; // Scale profit contribution

            // If decision is WAIT, profit is typically 0, which receives a neutral reward, but we can score WAIT quality separately.
            bool isTradingAction = sample.Decision.Action == DecisionAction.BUY || sample.Decision.Action == DecisionAction.SELL;

            // 2. RISK-ADJUSTED PERFORMANCE (Positive)
            // Evaluate reward relative to risk taken.
            if (isTradingAction && sample.Risk > 0.0)
            {
                // If trade is profitable, risk-adjusted reward is positive. If unprofitable, it is a negative drag.
                double riskRewardRatio = tradeProfitLoss / sample.Risk;
                breakdown.RiskAdjustedComponent = riskRewardRatio * 3.0;
            }

            // 3. DRAWDOWN PENALTY (Negative)
            // Penalize high drawdowns heavily.
            if (isTradingAction && sample.MaxDrawdown > 0.0)
            {
                // The larger the drawdown, the larger the penalty. We use a quadratic penalty to discourage extreme drawdowns.
                breakdown.DrawdownPenalty = -(Math.Pow(sample.MaxDrawdown, 1.5) * 0.5);
            }

            // 4. CORRECT MARKET PREDICTION (Positive/Negative)
            // Verify if the direction chosen matches the actual outcome.
            if (isTradingAction)
            {
                double priceDelta = sample.ExitPrice - sample.EntryPrice;
                bool isUpward = priceDelta > 0.0;
                bool isDownward = priceDelta < 0.0;

                if ((sample.Decision.Action == DecisionAction.BUY && isUpward) ||
                    (sample.Decision.Action == DecisionAction.SELL && isDownward))
                {
                    // Correct direction prediction bonus
                    breakdown.PredictionAccuracyComponent = 5.0 + (Math.Abs(priceDelta) * BaseScale * 0.02);
                }
                else if ((sample.Decision.Action == DecisionAction.BUY && isDownward) ||
                         (sample.Decision.Action == DecisionAction.SELL && isUpward))
                {
                    // Incorrect direction prediction penalty
                    breakdown.PredictionAccuracyComponent = -5.0 - (Math.Abs(priceDelta) * BaseScale * 0.02);
                }
            }

            // 5. GOOD TIMING COMPONENT (Positive or Negative)
            // For profitable trades, lower holding time is rewarded (efficiency of capital).
            // For unprofitable trades, high holding time represents holding onto a loser, which is penalized.
            if (isTradingAction)
            {
                if (tradeProfitLoss > 0.0)
                {
                    // Reward efficient entries (low holding time for high profit)
                    breakdown.TimingComponent = 2.0 / (1.0 + (sample.HoldingTimeMinutes / 60.0));
                }
                else if (tradeProfitLoss < 0.0)
                {
                    // Penalize holding losers (bleeding capital over time)
                    breakdown.TimingComponent = -(sample.HoldingTimeMinutes / 120.0);
                }
            }

            // 6. OVERTRADING PENALTY (Negative)
            // Penalize trading under low-confidence or high-noise environments (ranging/volatile regime with low confidence).
            if (isTradingAction)
            {
                bool isRanging = sample.MarketRegimeLabel.Contains("Ranging", StringComparison.OrdinalIgnoreCase) ||
                                 sample.MarketRegimeLabel.Contains("Volatile", StringComparison.OrdinalIgnoreCase);

                if (isRanging && sample.Confidence < 0.55)
                {
                    breakdown.OvertradingPenalty = -4.0;
                }
            }

            // 7. IGNORING UNCERTAINTY PENALTY (Negative)
            // Large risk taken when decision confidence is low.
            if (isTradingAction && sample.Confidence < 0.60 && sample.Risk > 1.5)
            {
                double excessRisk = sample.Risk - 1.5;
                breakdown.UncertaintyPenalty = -(excessRisk * 8.0);
            }

            // 8. RISK MANAGEMENT PENALTY (Negative)
            // Severe penalties for taking trades without stop loss or risking more than standard bounds (e.g. 3.0% of capital).
            if (isTradingAction)
            {
                if (sample.Risk == 0.0)
                {
                    breakdown.RiskManagementPenalty = -20.0; // Infinite risk penalty (no stop loss)
                }
                else if (sample.Risk > 3.0)
                {
                    // Excessive risk relative to safe trading rules
                    breakdown.RiskManagementPenalty = -(sample.Risk - 3.0) * 15.0;
                }
            }

            // Aggregated reward calculation
            breakdown.TotalReward = breakdown.ProfitComponent +
                                    breakdown.RiskAdjustedComponent +
                                    breakdown.DrawdownPenalty +
                                    breakdown.PredictionAccuracyComponent +
                                    breakdown.TimingComponent +
                                    breakdown.OvertradingPenalty +
                                    breakdown.UncertaintyPenalty +
                                    breakdown.RiskManagementPenalty;

            return breakdown;
        }
    }
}
