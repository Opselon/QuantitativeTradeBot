using Nexus.Core.Entities;

namespace Nexus.Core.DomainEvents
{
    /// <summary>
    /// Event raised when a new open trading position is successfully established on the account.
    /// </summary>
    public sealed class PositionOpenedEvent
    {
        public Guid EventId { get; }
        public Position Position { get; }
        public DateTime Timestamp { get; }

        public PositionOpenedEvent(Position position)
        {
            EventId = Guid.NewGuid();
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Timestamp = DateTime.UtcNow;
        }
    }
}
