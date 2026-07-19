using Nexus.Core.Entities;

namespace Nexus.Core.DomainEvents
{
    /// <summary>
    /// Event raised when a pre-trade risk or safety threshold has been breached.
    /// </summary>
    public sealed class RiskLimitReachedEvent
    {
        public Guid EventId { get; }
        public string ViolationReason { get; }
        public RiskState RiskState { get; }
        public DateTime Timestamp { get; }

        public RiskLimitReachedEvent(string violationReason, RiskState riskState)
        {
            EventId = Guid.NewGuid();
            ViolationReason = violationReason ?? throw new ArgumentNullException(nameof(violationReason));
            RiskState = riskState ?? throw new ArgumentNullException(nameof(riskState));
            Timestamp = DateTime.UtcNow;
        }
    }
}
