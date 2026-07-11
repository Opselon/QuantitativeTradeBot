using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedMt5ConnectionService : IMt5ConnectionService
    {
        public async Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            // Simulate networking delays
            await Task.Delay(1500, cancellationToken);

            if (string.IsNullOrWhiteSpace(profile.BrokerServer))
            {
                return new ConnectionTestResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Connection Failed: Broker server cannot be empty."
                };
            }

            if (string.IsNullOrWhiteSpace(profile.LoginAccountId))
            {
                return new ConnectionTestResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Connection Failed: Login account ID cannot be empty."
                };
            }

            if (string.IsNullOrWhiteSpace(profile.Password) || profile.Password.Length < 4)
            {
                return new ConnectionTestResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Connection Failed: Password is too short or missing."
                };
            }

            // Create a realistic snapshot
            var snapshot = new AccountSnapshotDto
            {
                AccountId = profile.LoginAccountId,
                BrokerServer = profile.BrokerServer,
                Balance = 10000.00m,
                Equity = 10000.00m,
                Margin = 0.00m,
                FreeMargin = 10000.00m,
                Leverage = 100,
                Currency = "USD",
                AccountMode = profile.BrokerServer.Contains("Demo", StringComparison.OrdinalIgnoreCase) ? "Demo" : "Real",
                TerminalStatus = "Connected"
            };

            return new ConnectionTestResultDto
            {
                IsSuccess = true,
                AccountSnapshot = snapshot
            };
        }

        public async Task<IMt5Session> CreateSessionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            var session = new SimulatedMt5Session();
            await session.ConnectAsync(cancellationToken);
            return session;
        }
    }
}
