using System;

namespace Nexus.Desktop.Models
{
    public class DesktopPositionDto
    {
        public long Ticket { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = "Buy";
        public decimal Volume { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal Profit { get; set; }
        public decimal Swap { get; set; }
        public decimal Commission { get; set; }
        public DateTime OpenTime { get; set; }
        public string Status { get; set; } = "Open";
    }
}
