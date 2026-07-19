using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedTradingPlatformConnector : ITradingPlatformConnector
    {
        private readonly IMt5ConnectionService _connectionService;

        public SimulatedTradingPlatformConnector(IMt5ConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public string PlatformName => "MetaTrader 5";

        public Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            return _connectionService.TestConnectionAsync(profile, cancellationToken);
        }
    }
}
