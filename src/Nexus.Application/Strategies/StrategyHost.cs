using Microsoft.Extensions.Logging;
using Nexus.Application.Observability;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Strategies
{
    public class StrategyHost : IStrategyHost
    {
        private readonly IStrategy _strategy;
        private readonly StrategyDescriptor _descriptor;
        private readonly ILogger<StrategyHost> _logger;
        private bool _isRunning;
        private bool _isPaused;

        public string StrategyId => _descriptor.StrategyId;
        public StrategyDescriptor Descriptor => _descriptor;
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;

        public StrategyHost(IStrategy strategy, StrategyDescriptor descriptor, ILogger<StrategyHost> logger)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync()
        {
            var context = WorkflowContext.Create("StrategyLifecycle", subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            using var scope = _logger.BeginWorkflowScope(context);

            try
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.StrategyStarted, "Initializing strategy '{StrategyName}' ({StrategyId})", _descriptor.Name, _descriptor.StrategyId);
                await _strategy.OnInitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Error during initialization of strategy '{StrategyId}'", _descriptor.StrategyId);
                throw;
            }
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            _isPaused = false;

            var context = WorkflowContext.Create("StrategyLifecycle", subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            using var scope = _logger.BeginWorkflowScope(context);

            _logger.LogStructured(LogLevel.Information, LogEventIds.StrategyStarted, "Strategy '{StrategyId}' started.", _descriptor.StrategyId);
            await Task.CompletedTask;
        }

        public async Task PauseAsync()
        {
            if (!_isRunning) return;
            _isPaused = true;

            var context = WorkflowContext.Create("StrategyLifecycle", subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            using var scope = _logger.BeginWorkflowScope(context);

            _logger.LogStructured(LogLevel.Information, LogEventIds.StrategyStarted, "Strategy '{StrategyId}' paused.", _descriptor.StrategyId);
            await Task.CompletedTask;
        }

        public async Task ResumeAsync()
        {
            if (!_isRunning) return;
            _isPaused = false;

            var context = WorkflowContext.Create("StrategyLifecycle", subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            using var scope = _logger.BeginWorkflowScope(context);

            _logger.LogStructured(LogLevel.Information, LogEventIds.StrategyStarted, "Strategy '{StrategyId}' resumed.", _descriptor.StrategyId);
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            var context = WorkflowContext.Create("StrategyLifecycle", subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            using var scope = _logger.BeginWorkflowScope(context);

            try
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.StrategyStopped, "Stopping strategy '{StrategyId}'", _descriptor.StrategyId);
                await _strategy.OnStopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Error during stopping strategy '{StrategyId}'", _descriptor.StrategyId);
            }
            finally
            {
                _isRunning = false;
                _isPaused = false;
            }
        }

        public async Task ProcessTickAsync(Tick tick, string correlationId)
        {
            if (!_isRunning || _isPaused) return;

            // Only process if the strategy is subscribed to this symbol
            if (!_descriptor.SubscribedSymbols.Contains(tick.Symbol.Name)) return;

            var context = WorkflowContext.Create("StrategyTickProcessing", correlationId, subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            context.Symbol = tick.Symbol.Name;
            using var scope = _logger.BeginWorkflowScope(context);

            try
            {
                _logger.LogStructured(LogLevel.Debug, LogEventIds.MarketDataReceived, "Dispatching Tick to strategy '{StrategyId}': {Tick}", _descriptor.StrategyId, tick);
                await _strategy.OnTickAsync(tick);
            }
            catch (Exception ex)
            {
                // Fault Containment: Single strategy crash must not bring down the entire engine
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Fault in strategy '{StrategyId}' while processing Tick: {Tick}", _descriptor.StrategyId, tick);
            }
        }

        public async Task ProcessBarAsync(Bar bar, string correlationId)
        {
            if (!_isRunning || _isPaused) return;

            if (!_descriptor.SubscribedSymbols.Contains(bar.Symbol.Name)) return;

            var context = WorkflowContext.Create("StrategyBarProcessing", correlationId, subsystem: "Strategy");
            context.StrategyId = _descriptor.StrategyId;
            context.Symbol = bar.Symbol.Name;
            using var scope = _logger.BeginWorkflowScope(context);

            try
            {
                _logger.LogStructured(LogLevel.Debug, LogEventIds.MarketDataReceived, "Dispatching Bar to strategy '{StrategyId}': {Bar}", _descriptor.StrategyId, bar);
                await _strategy.OnBarAsync(bar);
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.StrategyFailed, "Fault in strategy '{StrategyId}' while processing Bar: {Bar}", _descriptor.StrategyId, bar);
            }
        }
    }
}
