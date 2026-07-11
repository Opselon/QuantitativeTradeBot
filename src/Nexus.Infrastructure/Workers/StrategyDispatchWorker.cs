using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Strategies;
using Nexus.Application.Observability;
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
            _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerStartup, "Strategy Dispatch Worker is starting...");

            try
            {
                while (await _tickChannelReader.WaitToReadAsync(stoppingToken))
                {
                    while (_tickChannelReader.TryRead(out var tick))
                    {
                        string correlationId = Guid.NewGuid().ToString("N");

                        var context = WorkflowContext.Create("StrategyDispatch", correlationId, subsystem: "Strategy");
                        context.Symbol = tick.Symbol.Name;

                        using var scope = _logger.BeginWorkflowScope(context);

                        _logger.LogStructured(LogLevel.Debug, LogEventIds.MarketDataReceived,
                            "Dispatching tick to strategies: {Tick}", tick);

                        // Hand off to the supervisor (each strategy executes inside try-catch fault containment)
                        await _strategySupervisor.RouteTickAsync(tick, correlationId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Strategy Dispatch Worker cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Strategy Dispatch Worker encountered a critical failure.");
            }
            finally
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Strategy Dispatch Worker stopped.");
            }
        }
    }
}
