namespace Nexus.Application.Strategies
{
    public class StrategyExecutionContext
    {
        public string StrategyId { get; }
        public string CorrelationId { get; set; } = string.Empty;
        public bool IsSimulation { get; set; }

        public StrategyExecutionContext(string strategyId)
        {
            StrategyId = strategyId;
        }
    }
}
