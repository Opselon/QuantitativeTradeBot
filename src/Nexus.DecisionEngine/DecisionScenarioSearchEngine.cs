using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Service contract for a Stockfish-inspired tree-based scenario search and evaluation engine
    /// that supports an extended action space (BUY, SELL, WAIT, PARTIAL_CLOSE, FULL_CLOSE, MOVE_SL, MOVE_TP, REDUCE, ADD).
    /// </summary>
    public interface IDecisionScenarioSearchEngine
    {
        Task<ScenarioSearchNode> SearchBestActionAsync(
            MarketState currentState,
            RiskState riskState,
            double neuralBuyConfidence,
            double neuralSellConfidence,
            CancellationToken ct);
    }

    public sealed class DecisionScenarioSearchEngine : IDecisionScenarioSearchEngine
    {
        public Task<ScenarioSearchNode> SearchBestActionAsync(
            MarketState currentState,
            RiskState riskState,
            double neuralBuyConfidence,
            double neuralSellConfidence,
            CancellationToken ct)
        {
            if (currentState == null)
            {
                throw new ArgumentNullException(nameof(currentState));
            }

            // Standardize simulated parameters based on symbol specifications
            double currentPrice = 1.0800;
            double pipsValueMultiplier = 0.0001;

            if (currentState.Symbol.Contains("JPY"))
            {
                currentPrice = 150.00;
                pipsValueMultiplier = 0.01;
            }

            // Expanded action candidates inspired by Stockfish principles
            var candidates = new[]
            {
                DecisionAction.BUY,
                DecisionAction.SELL,
                DecisionAction.WAIT,
                DecisionAction.CLOSE, // Treated as FULL CLOSE
                DecisionAction.REDUCE, // Treated as REDUCE POSITION / PARTIAL CLOSE
                DecisionAction.ADD // Treated as ADD POSITION
            };

            ScenarioSearchNode bestNode = null!;
            double bestScore = double.MinValue;

            foreach (var action in candidates)
            {
                ct.ThrowIfCancellationRequested();

                var node = new ScenarioSearchNode(action, depth: 1);

                // Simulate candidate futures paths
                SimulatePaths(node, currentState, currentPrice, pipsValueMultiplier, neuralBuyConfidence, neuralSellConfidence);

                // Score this candidate action based on simulated results
                EvaluateNodeScore(node, action, riskState, currentPrice, pipsValueMultiplier);

                if (node.Score > bestScore)
                {
                    bestScore = node.Score;
                    bestNode = node;
                }
            }

            return Task.FromResult(bestNode);
        }

        private void SimulatePaths(
            ScenarioSearchNode node,
            MarketState state,
            double currentPrice,
            double pipsMultiplier,
            double buyConf,
            double sellConf)
        {
            double baseVolatilityPips = Math.Max(5.0, state.Volatility * 50.0);
            double baseMomentumPips = state.Momentum * 30.0;

            for (int i = 1; i <= 5; i++)
            {
                double priceOffsetPips = 0.0;
                double prob = 0.2;
                string regime = state.MarketRegime;

                switch (i)
                {
                    case 1: // Trend Continuation
                        priceOffsetPips = (node.Action == DecisionAction.BUY || node.Action == DecisionAction.ADD) ? (baseMomentumPips + 15.0) :
                                          (node.Action == DecisionAction.SELL) ? (-baseMomentumPips - 15.0) : 0.0;
                        prob = Math.Max(0.1, 0.4 * (node.Action == DecisionAction.BUY || node.Action == DecisionAction.ADD ? buyConf : (node.Action == DecisionAction.SELL ? sellConf : 0.5)));
                        regime = "TrendContinuation";
                        break;

                    case 2: // Reversal
                        priceOffsetPips = (node.Action == DecisionAction.BUY || node.Action == DecisionAction.ADD) ? (-baseMomentumPips - 20.0) :
                                          (node.Action == DecisionAction.SELL) ? (baseMomentumPips + 20.0) : 5.0;
                        prob = 0.2;
                        regime = "Reversal";
                        break;

                    case 3: // Volatility Expansion
                        double volShock = baseVolatilityPips * 1.8;
                        priceOffsetPips = (node.Action == DecisionAction.BUY || node.Action == DecisionAction.ADD) ? (volShock - 10.0) :
                                          (node.Action == DecisionAction.SELL) ? (-volShock + 10.0) : 25.0;
                        prob = 0.15;
                        regime = "VolatilityExpansion";
                        break;

                    case 4: // Liquidity Failure (slippage against trade)
                        priceOffsetPips = (node.Action == DecisionAction.BUY || node.Action == DecisionAction.ADD) ? (-15.0) :
                                          (node.Action == DecisionAction.SELL) ? (15.0) : 0.0;
                        prob = 0.15;
                        regime = "LiquidityFailure";
                        break;

                    case 5: // Sideways Consolidation
                        priceOffsetPips = 2.0;
                        prob = 0.1;
                        regime = "Consolidation";
                        break;
                }

                double projPrice = currentPrice + (priceOffsetPips * pipsMultiplier);
                double drawdown = Math.Max(0.0, (node.Action == DecisionAction.BUY || node.Action == DecisionAction.ADD) ? -priceOffsetPips : (node.Action == DecisionAction.SELL ? priceOffsetPips : 0.0));

                var scenario = new MarketStateScenario(
                    state.Symbol,
                    DateTime.UtcNow.AddMinutes(15 * i),
                    projPrice,
                    prob,
                    state.Volatility,
                    drawdown,
                    regime
                );

                node.AddScenario(scenario);
            }
        }

        private void EvaluateNodeScore(
            ScenarioSearchNode node,
            DecisionAction action,
            RiskState risk,
            double startPrice,
            double pipsMultiplier)
        {
            if (action == DecisionAction.WAIT)
            {
                node.ExpectedValue = 0.0;
                node.ProbabilityOfTakeProfit = 0.0;
                node.ProbabilityOfStopLoss = 0.0;
                node.MaxDrawdown = 0.0;
                node.TimeToResolutionMinutes = 0.0;
                node.Score = 0.1; // Baseline positive score for waiting/maintaining status quo
                node.Reasoning = "Hold current state. No aggressive action justified.";
                return;
            }

            if (action == DecisionAction.CLOSE)
            {
                // Closing positions reduces exposure to zero
                node.ExpectedValue = 0.0;
                node.ProbabilityOfTakeProfit = 0.0;
                node.ProbabilityOfStopLoss = 0.0;
                node.MaxDrawdown = 0.0;
                node.TimeToResolutionMinutes = 0.0;
                node.Score = risk != null && risk.IsTradingBlocked ? 1.0 : 0.2; // Prefer closing if blocked
                node.Reasoning = "Full close executed to eliminate downside market risk.";
                return;
            }

            if (action == DecisionAction.REDUCE)
            {
                node.ExpectedValue = 0.0;
                node.ProbabilityOfTakeProfit = 0.0;
                node.ProbabilityOfStopLoss = 0.0;
                node.MaxDrawdown = 0.0;
                node.TimeToResolutionMinutes = 0.0;
                node.Score = risk != null && risk.TotalExposure > 3.0 ? 0.4 : 0.15; // Prefer reducing exposure when high
                node.Reasoning = "Partial close / position size reduction to lock in fractional utility.";
                return;
            }

            double totalEv = 0.0;
            double tpCount = 0.0;
            double slCount = 0.0;
            double maxDrawdown = 0.0;
            double totalProb = 0.0;

            foreach (var sc in node.ProjectedScenarios)
            {
                totalProb += sc.Probability;
                double outcomePips = 0.0;
                if (action == DecisionAction.BUY || action == DecisionAction.ADD)
                {
                    outcomePips = (sc.ProjectedPrice - startPrice) / pipsMultiplier;
                }
                else if (action == DecisionAction.SELL)
                {
                    outcomePips = (startPrice - sc.ProjectedPrice) / pipsMultiplier;
                }

                totalEv += outcomePips * sc.Probability;
                maxDrawdown = Math.Max(maxDrawdown, sc.DrawdownRisk);

                if (outcomePips > 10.0) tpCount += sc.Probability;
                else if (outcomePips < -15.0) slCount += sc.Probability;
            }

            if (totalProb > 0)
            {
                node.ExpectedValue = totalEv / totalProb;
                node.ProbabilityOfTakeProfit = tpCount / totalProb;
                node.ProbabilityOfStopLoss = slCount / totalProb;
            }
            node.MaxDrawdown = maxDrawdown;
            node.TimeToResolutionMinutes = 45.0;

            // EV penalizing stop-loss probabilities and drawdowns (Forex scaling: MaxDrawdown * 0.01)
            double baseScore = node.ExpectedValue * (1.0 - node.ProbabilityOfStopLoss);
            double riskPenalty = (node.MaxDrawdown * 0.01) + (node.ProbabilityOfStopLoss * 0.2);

            if (risk != null && risk.IsTradingBlocked && (action == DecisionAction.BUY || action == DecisionAction.SELL || action == DecisionAction.ADD))
            {
                node.Score = -100.0;
                node.Reasoning = "Action blocked due to active pre-trade risk restrictions.";
            }
            else
            {
                node.Score = baseScore - riskPenalty;
                // Penalize adding to position slightly as it increases exposure risk
                if (action == DecisionAction.ADD)
                {
                    node.Score -= 0.5;
                }
                node.Reasoning = $"Simulated EV: {node.ExpectedValue:F1} pips. Score: {node.Score:F2}.";
            }
        }
    }
}
