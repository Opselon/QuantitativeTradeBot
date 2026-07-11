using System;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    public enum OrderDirection
    {
        Buy,
        Sell
    }

    public enum OrderType
    {
        Market,
        Limit,
        Stop
    }

    public enum OrderStatus
    {
        Pending,
        Filled,
        Rejected,
        Cancelled
    }

    public sealed class Order
    {
        public Guid Id { get; }
        public string TicketId { get; private set; }
        public Symbol Symbol { get; }
        public OrderDirection Direction { get; }
        public OrderType Type { get; }
        public LotSize Volume { get; }
        public double Price { get; }
        public double? StopLoss { get; private set; }
        public double? TakeProfit { get; private set; }
        public OrderStatus Status { get; private set; }
        public string StatusReason { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }

        public Order(Guid id, string ticketId, Symbol symbol, OrderDirection direction, OrderType type, LotSize volume, double price, double? stopLoss = null, double? takeProfit = null, OrderStatus status = OrderStatus.Pending)
        {
            Id = id;
            TicketId = ticketId ?? string.Empty;
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Direction = direction;
            Type = type;
            Volume = volume;
            Price = price;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            Status = status;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
        }

        public static Order CreateNew(Symbol symbol, OrderDirection direction, OrderType type, LotSize volume, double price, double? stopLoss = null, double? takeProfit = null)
        {
            return new Order(Guid.NewGuid(), string.Empty, symbol, direction, type, volume, price, stopLoss, takeProfit, OrderStatus.Pending);
        }

        public void Fill(string ticketId, double executionPrice)
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Cannot fill order in state: {Status}");

            TicketId = ticketId;
            Status = OrderStatus.Filled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject(string reason)
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Cannot reject order in state: {Status}");

            Status = OrderStatus.Rejected;
            StatusReason = reason ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Cannot cancel order in state: {Status}");

            Status = OrderStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ModifySlTp(double? sl, double? tp)
        {
            StopLoss = sl;
            TakeProfit = tp;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
