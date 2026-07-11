using System;
using Nexus.Core.Entities;

namespace Nexus.Infrastructure.Persistence.Models
{
    public class AccountDbModel
    {
        public Guid Id { get; set; }
        public string BrokerAccountId { get; set; } = string.Empty;
        public string BrokerName { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Balance { get; set; }
        public decimal Equity { get; set; }
        public decimal Margin { get; set; }
        public decimal FreeMargin { get; set; }
        public int Leverage { get; set; }
        public bool IsLive { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public Account ToDomain()
        {
            var account = new Account(
                Id,
                BrokerAccountId,
                BrokerName,
                Currency,
                Balance,
                Equity,
                Margin,
                FreeMargin,
                Leverage,
                IsLive
            );
            // Sync updated_at since domain entity sets UpdatedAt to UtcNow inside constructor
            var field = typeof(Account).GetProperty(nameof(Account.UpdatedAt));
            if (field != null && field.CanWrite)
            {
                field.SetValue(account, UpdatedAtUtc);
            }
            else
            {
                // Backup using reflection on backing field if property is read-only
                var backingField = typeof(Account).GetField($"<{nameof(Account.UpdatedAt)}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                backingField?.SetValue(account, UpdatedAtUtc);
            }
            return account;
        }

        public static AccountDbModel FromDomain(Account account)
        {
            return new AccountDbModel
            {
                Id = account.Id,
                BrokerAccountId = account.BrokerAccountId,
                BrokerName = account.BrokerName,
                Currency = account.Currency,
                Balance = account.Balance,
                Equity = account.Equity,
                Margin = account.Margin,
                FreeMargin = account.FreeMargin,
                Leverage = account.Leverage,
                IsLive = account.IsLive,
                UpdatedAtUtc = account.UpdatedAt.Kind == DateTimeKind.Utc ? account.UpdatedAt : DateTime.SpecifyKind(account.UpdatedAt, DateTimeKind.Utc)
            };
        }
    }
}
