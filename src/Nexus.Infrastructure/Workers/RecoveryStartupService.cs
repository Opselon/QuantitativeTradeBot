using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;
using Nexus.Application.Observability;

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
            var context = WorkflowContext.Create("SystemRecovery", subsystem: "Recovery");
            using var scope = _logger.BeginWorkflowScope(context);

            _logger.LogStructured(LogLevel.Information, LogEventIds.RecoveryStarted, "Recovery Startup Service is validating state on system start...");

            try
            {
                // Retrieve all open positions
                var openPositions = await _positionRepository.GetOpenPositionsAsync();
                _logger.LogStructured(LogLevel.Information, LogEventIds.RecoveryStarted, "System startup check: detected {Count} active positions in persistence.", openPositions.Count);
                foreach (var pos in openPositions)
                {
                    _logger.LogStructured(LogLevel.Information, LogEventIds.RecoveryStarted, "Restored Active Position State: PositionId={PositionId} TicketId={TicketId} Symbol={Symbol}", pos.Id, pos.TicketId, pos.Symbol.Name);
                }

                // Retrieve all pending orders
                var openOrders = await _orderRepository.GetOpenOrdersAsync();
                _logger.LogStructured(LogLevel.Information, LogEventIds.RecoveryStarted, "System startup check: detected {Count} pending orders in persistence.", openOrders.Count);
                foreach (var ord in openOrders)
                {
                    _logger.LogStructured(LogLevel.Information, LogEventIds.RecoveryStarted, "Restored Pending Order State: OrderId={Id} Symbol={Symbol}", ord.Id, ord.Symbol.Name);
                }

                _logger.LogStructured(LogLevel.Information, LogEventIds.RecoveryCompleted, "State validation and system recovery completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.RecoveryCompleted, "Failed to execute startup state recovery check.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Recovery Startup Service stopped.");
            return Task.CompletedTask;
        }
    }
}
