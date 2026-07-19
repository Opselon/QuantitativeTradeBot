using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Infrastructure.Persistence.Models
{
    public class OrderDbModel
    {
        public Guid Id { get; set; }
        public string TicketId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Direction { get; set; } = "Buy";
        public string Type { get; set; } = "Market";
        public decimal Volume { get; set; }
        public double Price { get; set; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public string Status { get; set; } = "Pending";
        public string StatusReason { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public Guid? AccountId { get; set; }

        public Order ToDomain()
        {
            var symbolObj = new Symbol(Symbol);
            var directionEnum = Enum.Parse<OrderDirection>(Direction, true);
            var typeEnum = Enum.Parse<OrderType>(Type, true);
            var statusEnum = Enum.Parse<OrderStatus>(Status, true);
            var volumeLot = new LotSize((double)Volume);

            var order = new Order(
                Id,
                TicketId,
                symbolObj,
                directionEnum,
                typeEnum,
                volumeLot,
                Price,
                StopLoss,
                TakeProfit,
                statusEnum
            );

            // Set read-only / private set properties via reflection
            SetPrivateProperty(order, nameof(Order.StatusReason), StatusReason);
            SetPrivateProperty(order, nameof(Order.CreatedAt), CreatedAtUtc);
            SetPrivateProperty(order, nameof(Order.UpdatedAt), UpdatedAtUtc);

            return order;
        }

        public static OrderDbModel FromDomain(Order order, Guid? accountId = null)
        {
            return new OrderDbModel
            {
                Id = order.Id,
                TicketId = order.TicketId,
                Symbol = order.Symbol.Name,
                Direction = order.Direction.ToString(),
                Type = order.Type.ToString(),
                Volume = (decimal)order.Volume.Value,
                Price = order.Price,
                StopLoss = order.StopLoss,
                TakeProfit = order.TakeProfit,
                Status = order.Status.ToString(),
                StatusReason = order.StatusReason,
                CreatedAtUtc = order.CreatedAt.Kind == DateTimeKind.Utc ? order.CreatedAt : DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc),
                UpdatedAtUtc = order.UpdatedAt.Kind == DateTimeKind.Utc ? order.UpdatedAt : DateTime.SpecifyKind(order.UpdatedAt, DateTimeKind.Utc),
                AccountId = accountId
            };
        }

        private static void SetPrivateProperty<T>(object target, string propertyName, T value)
        {
            var property = typeof(Order).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
            }
            else
            {
                var backingField = typeof(Order).GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                backingField?.SetValue(target, value);
            }
        }
    }
}
