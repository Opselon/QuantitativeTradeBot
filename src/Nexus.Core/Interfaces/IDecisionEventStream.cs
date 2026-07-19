using Nexus.Core.DomainEvents;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Event stream hub providing real-time publishing and subscribing to core Decision Intelligence pipeline events.
    /// </summary>
    public interface IDecisionEventStream
    {
        event Action<DecisionCreatedEvent>? OnDecisionCreated;
        event Action<DecisionChangedEvent>? OnDecisionChanged;
        event Action<RiskAdjustedEvent>? OnRiskAdjusted;
        event Action<PositionManagementEvent>? OnPositionManagement;
        event Action<ExecutionCompletedEvent>? OnExecutionCompleted;

        void PublishDecisionCreated(DecisionCreatedEvent @event);
        void PublishDecisionChanged(DecisionChangedEvent @event);
        void PublishRiskAdjusted(RiskAdjustedEvent @event);
        void PublishPositionManagement(PositionManagementEvent @event);
        void PublishExecutionCompleted(ExecutionCompletedEvent @event);
    }
}
