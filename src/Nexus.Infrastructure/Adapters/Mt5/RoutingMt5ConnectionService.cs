using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RoutingMt5ConnectionService : IMt5ConnectionService
    {
        private readonly IAppConfigurationService _configService;
        private readonly SimulatedMt5ConnectionService _simulatedService;
        private readonly RealMt5BridgeConnectionService _realService;

        public RoutingMt5ConnectionService(
            IAppConfigurationService configService,
            SimulatedMt5ConnectionService simulatedService,
            RealMt5BridgeConnectionService realService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _simulatedService = simulatedService ?? throw new ArgumentNullException(nameof(simulatedService));
            _realService = realService ?? throw new ArgumentNullException(nameof(realService));
        }

        private IMt5ConnectionService GetActiveService()
        {
            var settings = _configService.GetSettings();
            return string.Equals(settings.Mt5Mode, "Real", StringComparison.OrdinalIgnoreCase)
                ? _realService
                : _simulatedService;
        }

        public Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            return GetActiveService().TestConnectionAsync(profile, cancellationToken);
        }

        public Task<IMt5Session> CreateSessionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            return GetActiveService().CreateSessionAsync(profile, cancellationToken);
        }
    }
}
