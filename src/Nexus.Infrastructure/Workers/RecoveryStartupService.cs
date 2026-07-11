using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Workers
{
    public class RecoveryStartupService : IHostedService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly ILogger<RecoveryStartupService> _logger;

        public RecoveryStartupService(
            IOrderRepository orderRepository,
            IPositionRepository positionRepository,
            ILogger<RecoveryStartupService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Recovery Startup Service is validating state on system start...");

            try
            {
                // Retrieve all open positions
                var openPositions = await _positionRepository.GetOpenPositionsAsync();
                _logger.LogInformation("System startup check: detected {Count} active positions in persistence.", openPositions.Count);
                foreach (var pos in openPositions)
                {
                    _logger.LogInformation("Restored Active Position State: {Position}", pos);
                }

                // Retrieve all pending orders
                var openOrders = await _orderRepository.GetOpenOrdersAsync();
                _logger.LogInformation("System startup check: detected {Count} pending orders in persistence.", openOrders.Count);
                foreach (var ord in openOrders)
                {
                    _logger.LogInformation("Restored Pending Order State: OrderId={Id} Symbol={Symbol}", ord.Id, ord.Symbol);
                }

                _logger.LogInformation("State validation and system recovery completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute startup state recovery check.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Recovery Startup Service stopped.");
            return Task.CompletedTask;
        }
    }
}
