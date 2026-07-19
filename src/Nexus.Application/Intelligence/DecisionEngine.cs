using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public class DecisionEngine : IDecisionEngine
    {
        private const double ConfidenceThreshold = 0.65;

        public TradeDecision Evaluate(EvaluationResult evaluation, MarketState market, RiskState risk)
        {
            DateTime now = DateTime.UtcNow;

            if (risk != null && risk.IsTradingBlocked)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    $"Trading is blocked by pre-trade risk management limits (Drawdown: {risk.CurrentDrawdown:P1}).",
                    now);
            }

            if (evaluation == null)
            {
                return new TradeDecision(
                    DecisionAction.WAIT,
                    0.0,
                    "No neural evaluation result available.",
                    now);
            }

            // Evaluate opportunities based on neural model confidences
            double buy = evaluation.BuyConfidence;
            double sell = evaluation.SellConfidence;
            double wait = evaluation.WaitConfidence;

            if (buy > sell && buy > wait && buy >= ConfidenceThreshold)
            {
                double targetVolume = CalculateTargetVolume(evaluation, risk);
                return new TradeDecision(
                    DecisionAction.BUY,
                    targetVolume,
                    $"Neural buy opportunity identified with {buy:P0} confidence (Regime: {evaluation.MarketRegime}).",
                    now);
            }

            if (sell > buy && sell > wait && sell >= ConfidenceThreshold)
            {
                double targetVolume = CalculateTargetVolume(evaluation, risk);
                return new TradeDecision(
                    DecisionAction.SELL,
                    targetVolume,
                    $"Neural sell opportunity identified with {sell:P0} confidence (Regime: {evaluation.MarketRegime}).",
                    now);
            }

            return new TradeDecision(
                DecisionAction.WAIT,
                0.0,
                $"Hold position. Evaluation: Buy={buy:P0}, Sell={sell:P0}, Wait={wait:P0} under threshold {ConfidenceThreshold:P0}.",
                now);
        }

        private static double CalculateTargetVolume(EvaluationResult evaluation, RiskState? risk)
        {
            double baseVolume = 0.10;
            if (risk == null) return baseVolume;

            // Reduce volume if exposure is high
            if (risk.TotalExposure > 5.0)
            {
                baseVolume = 0.05;
            }
            if (risk.OpenTradeCount >= 3)
            {
                baseVolume = 0.02;
            }

            // Scale volume based on neural confidence
            double multiplier = evaluation.Confidence >= 0.85 ? 1.5 : (evaluation.Confidence < 0.70 ? 0.5 : 1.0);
            return Math.Round(baseVolume * multiplier, 2);
        }
    }
}
