using Nexus.Core.Entities;

namespace Nexus.Core.DomainEvents
{
    public sealed class MarginCallEvent
    {
        public Guid EventId { get; }
        public Account Account { get; }
        public double MarginLevelPercentage { get; }
        public DateTime Timestamp { get; }

        public MarginCallEvent(Account account, double marginLevelPercentage)
        {
            EventId = Guid.NewGuid();
            Account = account ?? throw new ArgumentNullException(nameof(account));
            MarginLevelPercentage = marginLevelPercentage;
            Timestamp = DateTime.UtcNow;
        }
    }
}
