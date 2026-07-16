using System;
using Nexus.Core.DomainEvents;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Dashboard
{
    public sealed class DecisionEventStream : IDecisionEventStream
    {
        public event Action<DecisionCreatedEvent>? OnDecisionCreated;
        public event Action<DecisionChangedEvent>? OnDecisionChanged;
        public event Action<RiskAdjustedEvent>? OnRiskAdjusted;
        public event Action<PositionManagementEvent>? OnPositionManagement;
        public event Action<ExecutionCompletedEvent>? OnExecutionCompleted;

        public void PublishDecisionCreated(DecisionCreatedEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            OnDecisionCreated?.Invoke(@event);
        }

        public void PublishDecisionChanged(DecisionChangedEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            OnDecisionChanged?.Invoke(@event);
        }

        public void PublishRiskAdjusted(RiskAdjustedEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            OnRiskAdjusted?.Invoke(@event);
        }

        public void PublishPositionManagement(PositionManagementEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            OnPositionManagement?.Invoke(@event);
        }

        public void PublishExecutionCompleted(ExecutionCompletedEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            OnExecutionCompleted?.Invoke(@event);
        }
    }
}
