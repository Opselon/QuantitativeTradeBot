using Nexus.Core.Entities;

namespace Nexus.Tests.Unit.Entities
{
    public class AccountTests
    {
        [Fact]
        public void Account_ShouldConstructAndUpdateMetricsSuccessfully()
        {
            var account = new Account(
                id: Guid.NewGuid(),
                brokerAccountId: "1025439",
                brokerName: "NEXUS_CAPITAL",
                currency: "USD",
                balance: 10000m,
                equity: 10000m,
                margin: 0m,
                freeMargin: 10000m,
                leverage: 500,
                isLive: false
            );

            Assert.Equal("1025439", account.BrokerAccountId);
            Assert.Equal("NEXUS_CAPITAL", account.BrokerName);
            Assert.Equal("USD", account.Currency);
            Assert.Equal(10000m, account.Balance);
            Assert.Equal(10000m, account.Equity);
            Assert.Equal(500, account.Leverage);
            Assert.False(account.IsLive);

            account.UpdateBalanceAndEquity(9500m, 9200m, 500m, 8700m);
            Assert.Equal(9500m, account.Balance);
            Assert.Equal(9200m, account.Equity);
            Assert.Equal(500m, account.Margin);
            Assert.Equal(8700m, account.FreeMargin);
        }

        [Fact]
        public void CalculateDrawdownPercentage_ShouldReturnCorrectMetrics()
        {
            var account = new Account(
                id: Guid.NewGuid(),
                brokerAccountId: "1025439",
                brokerName: "NEXUS_CAPITAL",
                currency: "USD",
                balance: 10000m,
                equity: 8000m,
                margin: 0m,
                freeMargin: 8000m,
                leverage: 500,
                isLive: false
            );

            // Initial deposit is $10,000, current Equity is $8,000.
            // Drawdown: $2,000 -> 20.0%
            double dd = account.CalculateDrawdownPercentage(10000m);
            Assert.Equal(20.0, dd);

            // If equity is greater than initial deposit, drawdown is 0%
            account.UpdateBalanceAndEquity(12000m, 11000m, 0m, 11000m);
            double dd2 = account.CalculateDrawdownPercentage(10000m);
            Assert.Equal(0.0, dd2);
        }
    }
}
