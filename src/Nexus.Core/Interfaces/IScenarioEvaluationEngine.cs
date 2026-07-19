using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for predicting and scoring alternative future scenarios.
    /// </summary>
    public interface IScenarioEvaluationEngine
    {
        ScenarioScore EvaluateScenarios(MarketState state);
    }
}
