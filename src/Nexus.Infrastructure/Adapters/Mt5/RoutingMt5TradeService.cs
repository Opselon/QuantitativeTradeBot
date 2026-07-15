using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RoutingMt5TradeService : IMt5TradeService
    {
        private readonly IAppConfigurationService _configService;
        private readonly SimulatedMt5TradeService _simulatedService;
        private readonly RealMt5BridgeAdapter _realService;

        public RoutingMt5TradeService(
            IAppConfigurationService configService,
            SimulatedMt5TradeService simulatedService,
            RealMt5BridgeAdapter realService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _simulatedService = simulatedService ?? throw new ArgumentNullException(nameof(simulatedService));
            _realService = realService ?? throw new ArgumentNullException(nameof(realService));
        }

        private IMt5TradeService GetActiveService()
        {
            var settings = _configService.GetSettings();

            bool isRealMode = string.Equals(settings.Mt5Mode, "Real", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(settings.Mt5Mode, "RealBridge", StringComparison.OrdinalIgnoreCase);

            return isRealMode ? _realService : _simulatedService;
        }

        public Task<PlaceOrderResponse> PlaceOrderAsync(IMt5Session session, PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            return GetActiveService().PlaceOrderAsync(session, request, cancellationToken);
        }

        public Task<ClosePositionResponse> ClosePositionAsync(IMt5Session session, ClosePositionRequest request, CancellationToken cancellationToken = default)
        {
            return GetActiveService().ClosePositionAsync(session, request, cancellationToken);
        }

        public Task<IReadOnlyList<BridgePositionDto>> GetOpenPositionsAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            return GetActiveService().GetOpenPositionsAsync(session, cancellationToken);
        }
    }
}