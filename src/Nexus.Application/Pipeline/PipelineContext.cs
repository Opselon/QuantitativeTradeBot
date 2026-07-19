namespace Nexus.Application.Pipeline
{
    public class PipelineContext
    {
        public string CorrelationId { get; }
        public string StrategyId { get; }
        public DateTime Timestamp { get; }

        public PipelineContext(string strategyId, string? correlationId = null)
        {
            StrategyId = strategyId;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
        }
    }
}
