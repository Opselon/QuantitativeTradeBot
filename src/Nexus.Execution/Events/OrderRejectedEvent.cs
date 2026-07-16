using System;
using Nexus.Execution.Domain;

namespace Nexus.Execution.Events
{
    public class OrderRejectedEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public OrderRequest Request { get; }
        public string Reason { get; }
        public double Latency { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public OrderRejectedEvent(OrderRequest request, string reason, double latency)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
            Latency = latency;
        }
    }
}
