using System;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.DomainEvents
{
    /// <summary>
    /// Event raised when an open trading position has been successfully closed.
    /// </summary>
    public sealed class PositionClosedEvent
    {
        public Guid EventId { get; }
        public Position Position { get; }
        public Price ClosePrice { get; }
        public decimal RealizedPnl { get; }
        public DateTime Timestamp { get; }

        public PositionClosedEvent(Position position, Price closePrice, decimal realizedPnl)
        {
            EventId = Guid.NewGuid();
            Position = position ?? throw new ArgumentNullException(nameof(position));
            ClosePrice = closePrice;
            RealizedPnl = realizedPnl;
            Timestamp = DateTime.UtcNow;
        }
    }
}
