using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedMt5AccountService : IMt5AccountService
    {
        public Task<AccountSnapshotDto> GetAccountSnapshotAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            // Simulate realistic MT5 account details
            var snapshot = new AccountSnapshotDto
            {
                AccountId = "7820491",
                BrokerServer = "ICMarkets-Demo",
                Balance = 50000.00m,
                Equity = 50245.50m,
                Margin = 1200.00m,
                FreeMargin = 49045.50m,
                Leverage = 500,
                Currency = "USD",
                AccountMode = "Demo",
                TerminalStatus = session.Status.ToString()
            };

            return Task.FromResult(snapshot);
        }
    }
}
