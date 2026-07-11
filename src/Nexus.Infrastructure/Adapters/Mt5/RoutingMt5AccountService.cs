using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RoutingMt5AccountService : IMt5AccountService
    {
        private readonly IAppConfigurationService _configService;
        private readonly SimulatedMt5AccountService _simulatedService;
        private readonly RealMt5BridgeAdapter _realService;

        public RoutingMt5AccountService(
            IAppConfigurationService configService,
            SimulatedMt5AccountService simulatedService,
            RealMt5BridgeAdapter realService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _simulatedService = simulatedService ?? throw new ArgumentNullException(nameof(simulatedService));
            _realService = realService ?? throw new ArgumentNullException(nameof(realService));
        }

        private IMt5AccountService GetActiveService()
        {
            var settings = _configService.GetSettings();
            return string.Equals(settings.Mt5Mode, "Real", StringComparison.OrdinalIgnoreCase)
                ? _realService
                : _simulatedService;
        }

        public Task<AccountSnapshotDto> GetAccountSnapshotAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            return GetActiveService().GetAccountSnapshotAsync(session, cancellationToken);
        }
    }
}
