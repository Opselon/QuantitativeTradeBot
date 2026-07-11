using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            try
            {
                _logger.LogInformation("Initializing strategy '{StrategyName}' ({StrategyId})", _descriptor.Name, _descriptor.StrategyId);
                await _strategy.OnInitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initialization of strategy '{StrategyId}'", _descriptor.StrategyId);
                throw;
            }
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            _isPaused = false;
            _logger.LogInformation("Strategy '{StrategyId}' started.", _descriptor.StrategyId);
            await Task.CompletedTask;
        }

        public async Task PauseAsync()
        {
            if (!_isRunning) return;
            _isPaused = true;
            _logger.LogInformation("Strategy '{StrategyId}' paused.", _descriptor.StrategyId);
            await Task.CompletedTask;
        }

        public async Task ResumeAsync()
        {
            if (!_isRunning) return;
            _isPaused = false;
            _logger.LogInformation("Strategy '{StrategyId}' resumed.", _descriptor.StrategyId);
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;
            try
            {
                _logger.LogInformation("Stopping strategy '{StrategyId}'", _descriptor.StrategyId);
                await _strategy.OnStopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stopping strategy '{StrategyId}'", _descriptor.StrategyId);
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

            try
            {
                _logger.LogDebug("[CorrID: {CorrelationId}] Dispatching Tick to strategy '{StrategyId}': {Tick}", correlationId, _descriptor.StrategyId, tick);
                await _strategy.OnTickAsync(tick);
            }
            catch (Exception ex)
            {
                // Fault Containment: Single strategy crash must not bring down the entire engine
                _logger.LogError(ex, "[CorrID: {CorrelationId}] Fault in strategy '{StrategyId}' while processing Tick: {Tick}", correlationId, _descriptor.StrategyId, tick);
            }
        }

        public async Task ProcessBarAsync(Bar bar, string correlationId)
        {
            if (!_isRunning || _isPaused) return;

            if (!_descriptor.SubscribedSymbols.Contains(bar.Symbol.Name)) return;

            try
            {
                _logger.LogDebug("[CorrID: {CorrelationId}] Dispatching Bar to strategy '{StrategyId}': {Bar}", correlationId, _descriptor.StrategyId, bar);
                await _strategy.OnBarAsync(bar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CorrID: {CorrelationId}] Fault in strategy '{StrategyId}' while processing Bar: {Bar}", correlationId, _descriptor.StrategyId, bar);
            }
        }
    }
}
