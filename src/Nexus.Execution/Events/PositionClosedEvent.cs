using System;

namespace Nexus.Execution.Events
{
    public class PositionClosedEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public string TicketId { get; }
        public double CloseVolume { get; }
        public double ClosePrice { get; }
        public decimal RealizedPnl { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public PositionClosedEvent(string ticketId, double closeVolume, double closePrice, decimal realizedPnl)
        {
            TicketId = ticketId ?? throw new ArgumentNullException(nameof(ticketId));
            CloseVolume = closeVolume;
            ClosePrice = closePrice;
            RealizedPnl = realizedPnl;
        }
    }
}
