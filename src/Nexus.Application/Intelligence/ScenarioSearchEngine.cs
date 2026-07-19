using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public sealed class ScenarioSearchEngine : IScenarioSearchEngine
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
            double currentPrice = 1.0800; // Realistic baseline price for standard currency pairs (like EURUSD)
            double pipsValueMultiplier = 0.0001; // Standard 1 pip value

            if (currentState.Symbol.Contains("JPY"))
            {
                currentPrice = 150.00;
                pipsValueMultiplier = 0.01;
            }
            else if (currentState.Symbol.Contains("USD") && currentState.Symbol.Length == 6)
            {
                currentPrice = 1.0800;
                pipsValueMultiplier = 0.0001;
            }

            // Candidate actions we want to search/evaluate
            var candidates = new[] { DecisionAction.BUY, DecisionAction.SELL, DecisionAction.WAIT };
            ScenarioSearchNode bestNode = null!;
            double bestScore = double.MinValue;

            foreach (var action in candidates)
            {
                ct.ThrowIfCancellationRequested();

                var node = new ScenarioSearchNode(action, depth: 1);

                // Run Monte Carlo / Probabilistic path simulations for this candidate action
                SimulatePaths(node, currentState, riskState, currentPrice, pipsValueMultiplier, neuralBuyConfidence, neuralSellConfidence);

                // Score this candidate action based on simulated results using dynamic price/pip scale
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
            RiskState risk,
            double currentPrice,
            double pipsMultiplier,
            double buyConf,
            double sellConf)
        {
            // We simulate 5 scenarios (distinct future market vectors / price outcomes)
            // Scenario 1: Clean continuation matching neural bias
            // Scenario 2: Counter-trend reversal
            // Scenario 3: Volatility expansion (wide range)
            // Scenario 4: Liquidity failure / execution slippage
            // Scenario 5: Micro-consolidation (flat noise)

            double baseVolatilityPips = Math.Max(5.0, state.Volatility * 50.0);
            double baseMomentumPips = state.Momentum * 30.0;

            for (int i = 1; i <= 5; i++)
            {
                double priceOffsetPips = 0.0;
                double prob = 0.2;
                string regime = state.MarketRegime;

                switch (i)
                {
                    case 1: // Continuation
                        priceOffsetPips = (node.Action == DecisionAction.BUY) ? (baseMomentumPips + 15.0) :
                                          (node.Action == DecisionAction.SELL) ? (-baseMomentumPips - 15.0) : 0.0;
                        prob = Math.Max(0.1, 0.4 * (node.Action == DecisionAction.BUY ? buyConf : (node.Action == DecisionAction.SELL ? sellConf : 0.5)));
                        regime = "TrendContinuation";
                        break;

                    case 2: // Reversal
                        priceOffsetPips = (node.Action == DecisionAction.BUY) ? (-baseMomentumPips - 20.0) :
                                          (node.Action == DecisionAction.SELL) ? (baseMomentumPips + 20.0) : (Random.Shared.NextDouble() > 0.5 ? 5.0 : -5.0);
                        prob = 0.2;
                        regime = "Reversal";
                        break;

                    case 3: // Volatility Expansion
                        double volShock = baseVolatilityPips * 1.8;
                        priceOffsetPips = (node.Action == DecisionAction.BUY) ? (volShock - 10.0) :
                                          (node.Action == DecisionAction.SELL) ? (-volShock + 10.0) : (Random.Shared.NextDouble() > 0.5 ? 25.0 : -25.0);
                        prob = 0.15;
                        regime = "VolatilityExpansion";
                        break;

                    case 4: // Liquidity Failure (slippage against trade)
                        priceOffsetPips = (node.Action == DecisionAction.BUY) ? (-15.0) :
                                          (node.Action == DecisionAction.SELL) ? (15.0) : 0.0;
                        prob = 0.15;
                        regime = "LiquidityFailure";
                        break;

                    case 5: // Micro-consolidation
                        priceOffsetPips = Random.Shared.Next(-3, 4);
                        prob = 0.1;
                        regime = "Consolidation";
                        break;
                }

                double projPrice = currentPrice + (priceOffsetPips * pipsMultiplier);
                double drawdown = Math.Max(0.0, node.Action == DecisionAction.BUY ? -priceOffsetPips : (node.Action == DecisionAction.SELL ? priceOffsetPips : 0.0));

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
                node.Score = 0.1; // Baseline positive score for waiting
                node.Reasoning = "Hold position. No trade justified by current scenario matrix.";
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
                // Reward/Risk mapping in pips using dynamic start price and pip scale
                double outcomePips = 0.0;
                if (action == DecisionAction.BUY)
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

            // Normalization
            if (totalProb > 0)
            {
                node.ExpectedValue = totalEv / totalProb;
                node.ProbabilityOfTakeProfit = tpCount / totalProb;
                node.ProbabilityOfStopLoss = slCount / totalProb;
            }
            node.MaxDrawdown = maxDrawdown;
            node.TimeToResolutionMinutes = 45.0;

            // Apply Stockfish scoring formula: EV penalizing high drawdowns and risk blockades
            double baseScore = node.ExpectedValue * (1.0 - node.ProbabilityOfStopLoss);
            double riskPenalty = (node.MaxDrawdown * 0.1) + (node.ProbabilityOfStopLoss * 0.5);

            if (risk != null && risk.IsTradingBlocked)
            {
                node.Score = -100.0;
                node.Reasoning = "Execution is blocked due to active pre-trade risk restrictions.";
            }
            else
            {
                node.Score = baseScore - riskPenalty;
                node.Reasoning = $"Simulated EV: {node.ExpectedValue:F1} pips. P(TP): {node.ProbabilityOfTakeProfit:P0}, MaxDD: {node.MaxDrawdown:F1} pips. Score: {node.Score:F2}.";
            }
        }
    }
}
