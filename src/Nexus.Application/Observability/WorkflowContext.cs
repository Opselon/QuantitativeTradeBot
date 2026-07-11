using System;

namespace Nexus.Application.Observability
{
    public class WorkflowContext
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public string Workflow { get; set; } = string.Empty;
        public string? StrategyId { get; set; }
        public string? Symbol { get; set; }
        public string? AccountId { get; set; }
        public string? OrderId { get; set; }
        public string? PositionId { get; set; }
        public string? Gateway { get; set; }
        public string? Subsystem { get; set; }

        public static WorkflowContext Create(
            string workflow,
            string? correlationId = null,
            string? operationId = null,
            string? subsystem = null)
        {
            return new WorkflowContext
            {
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
                OperationId = operationId ?? Guid.NewGuid().ToString("N"),
                Workflow = workflow,
                Subsystem = subsystem
            };
        }
    }
}
