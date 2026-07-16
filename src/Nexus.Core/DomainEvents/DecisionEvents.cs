using System;

namespace Nexus.Core.DomainEvents
{
    /// <summary>
    /// Event raised when a new TradeDecision is created/evaluated by the decision engine.
    /// </summary>
    public sealed class DecisionCreatedEvent
    {
        public Guid DecisionId { get; }
        public string Symbol { get; }
        public string Action { get; }
        public double Confidence { get; }
        public string Reason { get; }
        public DateTime TimestampUtc { get; }

        public DecisionCreatedEvent(Guid decisionId, string symbol, string action, double confidence, string reason)
        {
            DecisionId = decisionId;
            Symbol = symbol ?? "UNKNOWN";
            Action = action ?? "WAIT";
            Confidence = confidence;
            Reason = reason ?? string.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event raised when the core decision transitions or changes.
    /// </summary>
    public sealed class DecisionChangedEvent
    {
        public Guid DecisionId { get; }
        public string PreviousAction { get; }
        public string NewAction { get; }
        public double Confidence { get; }
        public string Reason { get; }
        public DateTime TimestampUtc { get; }

        public DecisionChangedEvent(Guid decisionId, string previousAction, string newAction, double confidence, string reason)
        {
            DecisionId = decisionId;
            PreviousAction = previousAction ?? "WAIT";
            NewAction = newAction ?? "WAIT";
            Confidence = confidence;
            Reason = reason ?? string.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event raised when pre-trade or active-trade risk thresholds are dynamically adjusted.
    /// </summary>
    public sealed class RiskAdjustedEvent
    {
        public Guid DecisionId { get; }
        public string RiskMetric { get; }
        public double PreviousValue { get; }
        public double NewValue { get; }
        public string Reason { get; }
        public DateTime TimestampUtc { get; }

        public RiskAdjustedEvent(Guid decisionId, string riskMetric, double previousValue, double newValue, string reason)
        {
            DecisionId = decisionId;
            RiskMetric = riskMetric ?? "GeneralRisk";
            PreviousValue = previousValue;
            NewValue = newValue;
            Reason = reason ?? string.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event raised when active positions are adjusted, scaled, or trailing parameters are updated.
    /// </summary>
    public sealed class PositionManagementEvent
    {
        public Guid PositionId { get; }
        public string Symbol { get; }
        public string ActionType { get; } // MOVE_SL, MOVE_TP, PARTIAL_CLOSE, REDUCE, etc.
        public double Volume { get; }
        public string Reason { get; }
        public DateTime TimestampUtc { get; }

        public PositionManagementEvent(Guid positionId, string symbol, string actionType, double volume, string reason)
        {
            PositionId = positionId;
            Symbol = symbol ?? "UNKNOWN";
            ActionType = actionType ?? "MODIFY";
            Volume = volume;
            Reason = reason ?? string.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event raised when trade execution is completed on MT5 or simulation adapters.
    /// </summary>
    public sealed class ExecutionCompletedEvent
    {
        public Guid DecisionId { get; }
        public string Symbol { get; }
        public string Action { get; }
        public double ExecutedPrice { get; }
        public double ExecutedVolume { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public DateTime TimestampUtc { get; }

        public ExecutionCompletedEvent(Guid decisionId, string symbol, string action, double executedPrice, double executedVolume, bool isSuccess, string errorMessage)
        {
            DecisionId = decisionId;
            Symbol = symbol ?? "UNKNOWN";
            Action = action ?? "WAIT";
            ExecutedPrice = executedPrice;
            ExecutedVolume = executedVolume;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }
}
