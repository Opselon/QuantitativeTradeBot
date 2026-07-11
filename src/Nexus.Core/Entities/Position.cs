using System;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    public sealed class Position
    {
        public Guid Id { get; }
        public string TicketId { get; }
        public Symbol Symbol { get; }
        public OrderDirection Direction { get; }
        public LotSize Volume { get; }
        public double EntryPrice { get; }
        public double CurrentPrice { get; private set; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public decimal UnrealizedPnl { get; private set; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }

        public Position(Guid id, string ticketId, Symbol symbol, OrderDirection direction, LotSize volume, double entryPrice, double currentPrice, double? stopLoss = null, double? takeProfit = null)
        {
            Id = id;
            TicketId = ticketId ?? throw new ArgumentNullException(nameof(ticketId));
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Direction = direction;
            Volume = volume;
            EntryPrice = entryPrice;
            CurrentPrice = currentPrice;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
            RecalculatePnl();
        }

        public void UpdatePrice(double currentPrice)
        {
            CurrentPrice = currentPrice;
            UpdatedAt = DateTime.UtcNow;
            RecalculatePnl();
        }

        public void ModifySlTp(double? stopLoss, double? takeProfit)
        {
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            UpdatedAt = DateTime.UtcNow;
        }

        private void RecalculatePnl()
        {
            // Standard Forex/CFD contract size is often 100,000 units. For gold (XAUUSD), it is usually 100 ounces per lot.
            // Let's use a standard default calculation logic or customizable multiplier based on Symbol name.
            double multiplier = GetContractMultiplier(Symbol.Name);
            double priceDiff = Direction == OrderDirection.Buy
                ? (CurrentPrice - EntryPrice)
                : (EntryPrice - CurrentPrice);

            double pnlDouble = priceDiff * Volume.Value * multiplier;
            // Round to 4 decimal places to avoid floating point precision artifacts before converting to decimal
            UnrealizedPnl = (decimal)Math.Round(pnlDouble, 4);
        }

        private static double GetContractMultiplier(string symbolName)
        {
            string upper = symbolName.ToUpperInvariant();
            if (upper.Contains("XAU") || upper.Contains("GOLD")) return 100.0; // 100 oz per standard Gold contract
            if (upper.Contains("XAG") || upper.Contains("SILVER")) return 5000.0; // 5000 oz per standard Silver contract
            return 100000.0; // Standard 100k contract for Forex (EURUSD, GBPUSD, etc.)
        }

        public override string ToString() => $"[Position {TicketId}] {Symbol} {Direction} {Volume} Entry={EntryPrice:F5} Current={CurrentPrice:F5} PnL={UnrealizedPnl:C2}";
    }
}
