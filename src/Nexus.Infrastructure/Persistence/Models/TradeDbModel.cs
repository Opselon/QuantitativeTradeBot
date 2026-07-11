using System;

namespace Nexus.Infrastructure.Persistence.Models
{
    public class TradeDbModel
    {
        public Guid Id { get; set; }
        public string TicketId { get; set; } = string.Empty;
        public Guid? PositionId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Direction { get; set; } = "Buy";
        public decimal Volume { get; set; }
        public double Price { get; set; }
        public decimal Commission { get; set; }
        public decimal Swap { get; set; }
        public decimal RealizedPnl { get; set; }
        public DateTime ExecutedAtUtc { get; set; }
    }
}
