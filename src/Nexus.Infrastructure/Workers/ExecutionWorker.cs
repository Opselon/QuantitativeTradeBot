using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Observability;
using Nexus.Application.Pipeline;
using System.Threading.Channels;

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
            _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerStartup, "Execution Worker is starting...");

            try
            {
                while (await _signalChannelReader.WaitToReadAsync(stoppingToken))
                {
                    while (_signalChannelReader.TryRead(out var signal))
                    {
                        string correlationId = Guid.NewGuid().ToString("N");

                        var workflowContext = WorkflowContext.Create("SignalExecution", correlationId, subsystem: "Execution");
                        workflowContext.StrategyId = signal.StrategyId;
                        workflowContext.Symbol = signal.SymbolName;

                        using var scope = _logger.BeginWorkflowScope(workflowContext);

                        _logger.LogStructured(LogLevel.Information, LogEventIds.SignalEmitted,
                            "Execution worker received trade signal from Strategy: {StrategyId} for {Symbol}",
                            signal.StrategyId, signal.SymbolName);

                        var context = new PipelineContext(signal.StrategyId, correlationId);
                        var result = await _signalRouter.RouteSignalAsync(signal, context, stoppingToken);

                        if (result.IsSuccess)
                        {
                            _logger.LogStructured(LogLevel.Information, LogEventIds.OrderFilled,
                                "Signal executed successfully. Ticket: {TicketId}", result.TicketId);
                        }
                        else
                        {
                            _logger.LogStructured(LogLevel.Warning, LogEventIds.OrderRejected,
                                "Signal execution failed: {Error}", result.ErrorMessage);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Execution Worker cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Execution Worker encountered a critical failure.");
            }
            finally
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Execution Worker stopped.");
            }
        }
    }
}
