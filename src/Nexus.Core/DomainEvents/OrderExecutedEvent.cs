using System;
using Nexus.Core.Entities;

namespace Nexus.Core.DomainEvents
{
    public sealed class OrderExecutedEvent
    {
        public Guid EventId { get; }
        public Order Order { get; }
        public double ExecutionPrice { get; }
        public string TicketId { get; }
        public DateTime Timestamp { get; }

        public OrderExecutedEvent(Order order, double executionPrice, string ticketId)
        {
            EventId = Guid.NewGuid();
            Order = order ?? throw new ArgumentNullException(nameof(order));
            ExecutionPrice = executionPrice;
            TicketId = ticketId ?? throw new ArgumentNullException(nameof(ticketId));
            Timestamp = DateTime.UtcNow;
        }
    }
}
