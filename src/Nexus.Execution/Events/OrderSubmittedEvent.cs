using System;
using Nexus.Execution.Domain;

namespace Nexus.Execution.Events
{
    public class OrderSubmittedEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public OrderRequest Request { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public OrderSubmittedEvent(OrderRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }
    }
}
