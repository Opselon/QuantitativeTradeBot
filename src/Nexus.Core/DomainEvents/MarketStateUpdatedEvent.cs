using System;
using Nexus.Core.Entities;

namespace Nexus.Core.DomainEvents
{
    /// <summary>
    /// Event raised when the classified internal market state representation is updated.
    /// </summary>
    public sealed class MarketStateUpdatedEvent
    {
        public Guid EventId { get; }
        public MarketState MarketState { get; }
        public DateTime Timestamp { get; }

        public MarketStateUpdatedEvent(MarketState marketState)
        {
            EventId = Guid.NewGuid();
            MarketState = marketState ?? throw new ArgumentNullException(nameof(marketState));
            Timestamp = DateTime.UtcNow;
        }
    }
}
