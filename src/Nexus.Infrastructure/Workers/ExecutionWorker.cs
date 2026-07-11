using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Pipeline;

namespace Nexus.Infrastructure.Workers
{
    public class ExecutionWorker : BackgroundService
    {
        private readonly ChannelReader<TradeSignal> _signalChannelReader;
        private readonly SignalRouter _signalRouter;
        private readonly ILogger<ExecutionWorker> _logger;

        public ExecutionWorker(
            ChannelReader<TradeSignal> signalChannelReader,
            SignalRouter signalRouter,
            ILogger<ExecutionWorker> logger)
        {
            _signalChannelReader = signalChannelReader ?? throw new ArgumentNullException(nameof(signalChannelReader));
            _signalRouter = signalRouter ?? throw new ArgumentNullException(nameof(signalRouter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Execution Worker is starting...");

            try
            {
                while (await _signalChannelReader.WaitToReadAsync(stoppingToken))
                {
                    while (_signalChannelReader.TryRead(out var signal))
                    {
                        string correlationId = Guid.NewGuid().ToString("N");
                        _logger.LogInformation("[CorrID: {CorrelationId}] Execution worker received trade signal from Strategy: {StrategyId}", correlationId, signal.StrategyId);

                        var context = new PipelineContext(signal.StrategyId, correlationId);
                        var result = await _signalRouter.RouteSignalAsync(signal, context, stoppingToken);

                        if (result.IsSuccess)
                        {
                            _logger.LogInformation("[CorrID: {CorrelationId}] Signal executed successfully. Ticket: {TicketId}", correlationId, result.TicketId);
                        }
                        else
                        {
                            _logger.LogWarning("[CorrID: {CorrelationId}] Signal execution failed: {Error}", correlationId, result.ErrorMessage);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Execution Worker cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Execution Worker encountered a critical failure.");
            }
            finally
            {
                _logger.LogInformation("Execution Worker stopped.");
            }
        }
    }
}
