using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RoutingMt5TradingService : IMt5TradingService
    {
        private readonly IAppConfigurationService _configService;
        private readonly SimulatedMt5TradingService _simulatedService;
        private readonly RealMt5TradingService _realService;

        public RoutingMt5TradingService(
            IAppConfigurationService configService,
            SimulatedMt5TradingService simulatedService,
            RealMt5TradingService realService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _simulatedService = simulatedService ?? throw new ArgumentNullException(nameof(simulatedService));
            _realService = realService ?? throw new ArgumentNullException(nameof(realService));
        }

        private IMt5TradingService GetActiveService()
        {
            var settings = _configService.GetSettings();
            return string.Equals(settings.Mt5Mode, "Real", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(settings.Mt5Mode, "RealBridge", StringComparison.OrdinalIgnoreCase)
                ? _realService
                : _simulatedService;
        }

        public Task<PlaceOrderResult> PlaceMarketOrderAsync(
            string symbol,
            BridgeOrderSide side,
            decimal volume,
            decimal? stopLoss,
            decimal? takeProfit,
            string? comment,
            string? clientCorrelationId,
            CancellationToken cancellationToken)
        {
            return GetActiveService().PlaceMarketOrderAsync(symbol, side, volume, stopLoss, takeProfit, comment, clientCorrelationId, cancellationToken);
        }

        public Task<ClosePositionResult> ClosePositionAsync(
            long positionTicket,
            string symbol,
            decimal? volume,
            CancellationToken cancellationToken)
        {
            return GetActiveService().ClosePositionAsync(positionTicket, symbol, volume, cancellationToken);
        }

        public Task<IReadOnlyList<OpenPositionDto>> GetOpenPositionsAsync(
            CancellationToken cancellationToken)
        {
            return GetActiveService().GetOpenPositionsAsync(cancellationToken);
        }
    }
}
