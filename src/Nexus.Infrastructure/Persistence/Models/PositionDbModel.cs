using System;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Infrastructure.Persistence.Models
{
    public class PositionDbModel
    {
        public Guid Id { get; set; }
        public string TicketId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Direction { get; set; } = "Buy";
        public decimal Volume { get; set; }
        public double EntryPrice { get; set; }
        public double CurrentPrice { get; set; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public string Status { get; set; } = "OPEN";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime OpenedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public Guid? AccountId { get; set; }

        public Position ToDomain()
        {
            var symbolObj = new Symbol(Symbol);
            var directionEnum = Enum.Parse<OrderDirection>(Direction, true);
            var volumeLot = new LotSize((double)Volume);

            var position = new Position(
                Id,
                TicketId,
                symbolObj,
                directionEnum,
                volumeLot,
                EntryPrice,
                CurrentPrice,
                StopLoss,
                TakeProfit
            );

            // Sync private / read-only fields via reflection
            SetPrivateProperty(position, nameof(Position.CreatedAt), CreatedAtUtc);
            SetPrivateProperty(position, nameof(Position.UpdatedAt), UpdatedAtUtc);
            SetPrivateProperty(position, nameof(Position.UnrealizedPnl), UnrealizedPnl);

            return position;
        }

        public static PositionDbModel FromDomain(Position position, Guid? accountId = null, string status = "OPEN")
        {
            return new PositionDbModel
            {
                Id = position.Id,
                TicketId = position.TicketId,
                Symbol = position.Symbol.Name,
                Direction = position.Direction.ToString(),
                Volume = (decimal)position.Volume.Value,
                EntryPrice = position.EntryPrice,
                CurrentPrice = position.CurrentPrice,
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                UnrealizedPnl = position.UnrealizedPnl,
                Status = status,
                CreatedAtUtc = position.CreatedAt.Kind == DateTimeKind.Utc ? position.CreatedAt : DateTime.SpecifyKind(position.CreatedAt, DateTimeKind.Utc),
                OpenedAtUtc = position.CreatedAt.Kind == DateTimeKind.Utc ? position.CreatedAt : DateTime.SpecifyKind(position.CreatedAt, DateTimeKind.Utc),
                UpdatedAtUtc = position.UpdatedAt.Kind == DateTimeKind.Utc ? position.UpdatedAt : DateTime.SpecifyKind(position.UpdatedAt, DateTimeKind.Utc),
                AccountId = accountId
            };
        }

        private static void SetPrivateProperty<T>(object target, string propertyName, T value)
        {
            var property = typeof(Position).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
            }
            else
            {
                var backingField = typeof(Position).GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                backingField?.SetValue(target, value);
            }
        }
    }
}
