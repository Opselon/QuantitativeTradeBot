namespace Nexus.Core.Entities
{
    public sealed class Account
    {
        public Guid Id { get; }
        public string BrokerAccountId { get; }
        public string BrokerName { get; }
        public string Currency { get; }
        public decimal Balance { get; private set; }
        public decimal Equity { get; private set; }
        public decimal Margin { get; private set; }
        public decimal FreeMargin { get; private set; }
        public int Leverage { get; }
        public bool IsLive { get; }
        public DateTime UpdatedAt { get; private set; }

        public Account(Guid id, string brokerAccountId, string brokerName, string currency, decimal balance, decimal equity, decimal margin, decimal freeMargin, int leverage, bool isLive)
        {
            if (string.IsNullOrWhiteSpace(brokerAccountId))
                throw new ArgumentException("Broker account ID cannot be empty.", nameof(brokerAccountId));
            if (string.IsNullOrWhiteSpace(brokerName))
                throw new ArgumentException("Broker name cannot be empty.", nameof(brokerName));

            Id = id;
            BrokerAccountId = brokerAccountId;
            BrokerName = brokerName;
            Currency = currency?.ToUpperInvariant() ?? "USD";
            Balance = balance;
            Equity = equity;
            Margin = margin;
            FreeMargin = freeMargin;
            Leverage = leverage;
            IsLive = isLive;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateBalanceAndEquity(decimal balance, decimal equity, decimal margin, decimal freeMargin)
        {
            Balance = balance;
            Equity = equity;
            Margin = margin;
            FreeMargin = freeMargin;
            UpdatedAt = DateTime.UtcNow;
        }

        public double CalculateDrawdownPercentage(decimal initialDeposit)
        {
            if (initialDeposit <= 0) return 0;
            if (Equity >= initialDeposit) return 0;

            decimal drawdown = initialDeposit - Equity;
            return (double)(drawdown / initialDeposit) * 100.0;
        }

        public override string ToString() => $"[Account {BrokerAccountId} - {BrokerName}] Balance={Balance:C2} Equity={Equity:C2} FreeMargin={FreeMargin:C2} Live={IsLive}";
    }
}
