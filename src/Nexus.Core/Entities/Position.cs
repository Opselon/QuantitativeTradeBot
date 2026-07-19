using Nexus.Core.Enums;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents an open or closed trading position.
    /// Integrates newly-defined Value Objects and Enums while keeping full backward compatibility.
    /// </summary>
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
        public PositionStatus Status { get; private set; }

        #region Value Object properties

        public Price EntryPriceObj => new Price(EntryPrice);
        public Price CurrentPriceObj => new Price(CurrentPrice);
        public Price? StopLossObj => StopLoss.HasValue ? new Price(StopLoss.Value) : null;
        public Price? TakeProfitObj => TakeProfit.HasValue ? new Price(TakeProfit.Value) : null;
        public OrderSide Side => Direction == OrderDirection.Buy ? OrderSide.Buy : OrderSide.Sell;

        #endregion

        public Position(
            Guid id,
            string ticketId,
            Symbol symbol,
            OrderDirection direction,
            LotSize volume,
            double entryPrice,
            double currentPrice,
            double? stopLoss = null,
            double? takeProfit = null,
            PositionStatus status = PositionStatus.Open)
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
            Status = status;
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

        /// <summary>
        /// Transitions the position's status to Closed and updates its current price.
        /// </summary>
        public void Close(Price closePrice)
        {
            CurrentPrice = closePrice.Value;
            Status = PositionStatus.Closed;
            UpdatedAt = DateTime.UtcNow;
            RecalculatePnl();
        }

        private void RecalculatePnl()
        {
            double multiplier = GetContractMultiplier(Symbol.Name);
            double priceDiff = Direction == OrderDirection.Buy
                ? (CurrentPrice - EntryPrice)
                : (EntryPrice - CurrentPrice);

            double pnlDouble = priceDiff * Volume.Value * multiplier;
            UnrealizedPnl = (decimal)Math.Round(pnlDouble, 4);
        }

        private static double GetContractMultiplier(string symbolName)
        {
            string upper = symbolName.ToUpperInvariant();
            if (upper.Contains("XAU") || upper.Contains("GOLD")) return 100.0;
            if (upper.Contains("XAG") || upper.Contains("SILVER")) return 5000.0;
            return 100000.0;
        }

        public override string ToString() =>
            $"[Position {TicketId}] {Symbol} {Direction} {Volume} Entry={EntryPrice:F5} Current={CurrentPrice:F5} PnL={UnrealizedPnl:C2} Status={Status}";
    }
}
