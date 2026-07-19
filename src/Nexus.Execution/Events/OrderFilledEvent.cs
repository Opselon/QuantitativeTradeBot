using Nexus.Execution.Domain;

namespace Nexus.Execution.Events
{
    public class OrderFilledEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public OrderRequest Request { get; }
        public string TicketId { get; }
        public double ExecutionPrice { get; }
        public double Latency { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public OrderFilledEvent(OrderRequest request, string ticketId, double executionPrice, double latency)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            TicketId = ticketId ?? throw new ArgumentNullException(nameof(ticketId));
            ExecutionPrice = executionPrice;
            Latency = latency;
        }
    }
}
