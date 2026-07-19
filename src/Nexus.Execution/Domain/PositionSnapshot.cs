namespace Nexus.Execution.Domain
{
    public class PositionSnapshot
    {
        public string TicketId { get; }
        public string Symbol { get; }
        public string Direction { get; }
        public double Volume { get; }
        public double EntryPrice { get; }
        public double CurrentPrice { get; set; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public double RiskExposure { get; set; } // Absolute risk exposure amount or percentage
        public string Status { get; set; } // "OPEN", "CLOSED" etc

        public PositionSnapshot(
            string ticketId,
            string symbol,
            string direction,
            double volume,
            double entryPrice,
            double currentPrice,
            double? stopLoss,
            double? takeProfit,
            decimal unrealizedPnl,
            double riskExposure,
            string status)
        {
            TicketId = ticketId ?? throw new ArgumentNullException(nameof(ticketId));
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Volume = volume;
            EntryPrice = entryPrice;
            CurrentPrice = currentPrice;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            UnrealizedPnl = unrealizedPnl;
            RiskExposure = riskExposure;
            Status = status ?? "OPEN";
        }
    }
}
