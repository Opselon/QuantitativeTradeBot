using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Strategies;
using Nexus.Core.Entities;

namespace Nexus.Infrastructure.Workers
{
    public class StrategyDispatchWorker : BackgroundService
    {
        private readonly ChannelReader<Tick> _tickChannelReader;
        private readonly StrategySupervisor _strategySupervisor;
        private readonly ILogger<StrategyDispatchWorker> _logger;

        public StrategyDispatchWorker(
            ChannelReader<Tick> tickChannelReader,
            StrategySupervisor strategySupervisor,
            ILogger<StrategyDispatchWorker> logger)
        {
            _tickChannelReader = tickChannelReader ?? throw new ArgumentNullException(nameof(tickChannelReader));
            _strategySupervisor = strategySupervisor ?? throw new ArgumentNullException(nameof(strategySupervisor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Strategy Dispatch Worker is starting...");

            try
            {
                while (await _tickChannelReader.WaitToReadAsync(stoppingToken))
                {
                    while (_tickChannelReader.TryRead(out var tick))
                    {
                        string correlationId = Guid.NewGuid().ToString("N");
                        _logger.LogDebug("[CorrID: {CorrelationId}] Dispatching tick to strategies: {Tick}", correlationId, tick);

                        // Hand off to the supervisor (each strategy executes inside try-catch fault containment)
                        await _strategySupervisor.RouteTickAsync(tick, correlationId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Strategy Dispatch Worker cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Strategy Dispatch Worker encountered a critical failure.");
            }
            finally
            {
                _logger.LogInformation("Strategy Dispatch Worker stopped.");
            }
        }
    }
}
