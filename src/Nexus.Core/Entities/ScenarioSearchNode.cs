using System;
using System.Collections.Generic;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a search-and-evaluation node in the decision tree, modeling a specific action (BUY/SELL/WAIT)
    /// and its simulated future probabilities, risk limits, and expected values.
    /// </summary>
    public sealed class ScenarioSearchNode
    {
        public DecisionAction Action { get; }
        public int Depth { get; }
        public double ExpectedValue { get; set; }
        public double ProbabilityOfTakeProfit { get; set; }
        public double ProbabilityOfStopLoss { get; set; }
        public double MaxDrawdown { get; set; }
        public double TimeToResolutionMinutes { get; set; }
        public double Score { get; set; }
        public string Reasoning { get; set; }

        public List<MarketStateScenario> ProjectedScenarios { get; } = new();

        public ScenarioSearchNode(DecisionAction action, int depth, string reasoning = "")
        {
            Action = action;
            Depth = depth;
            Reasoning = reasoning ?? string.Empty;
        }

        public void AddScenario(MarketStateScenario scenario)
        {
            if (scenario != null)
            {
                ProjectedScenarios.Add(scenario);
            }
        }
    }
}
