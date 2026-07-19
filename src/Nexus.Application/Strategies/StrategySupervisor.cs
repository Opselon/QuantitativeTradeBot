using Microsoft.Extensions.Logging;
using Nexus.Core.Entities;
using System.Collections.Concurrent;

namespace Nexus.Application.Strategies
{
    public class StrategySupervisor
    {
        private readonly ConcurrentDictionary<string, IStrategyHost> _hosts = new();
        private readonly ILogger<StrategySupervisor> _logger;
        private bool _isEngineRunning;

        public bool IsEngineRunning => _isEngineRunning;
        public IEnumerable<IStrategyHost> Hosts => _hosts.Values;

        public StrategySupervisor(ILogger<StrategySupervisor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void AddHost(IStrategyHost host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            _hosts[host.StrategyId] = host;
            _logger.LogInformation("Added strategy host for '{StrategyId}' to supervisor", host.StrategyId);
        }

        public async Task StartAllAsync()
        {
            _logger.LogInformation("Starting all hosted strategies...");
            foreach (var host in _hosts.Values)
            {
                await host.InitializeAsync();
                await host.StartAsync();
            }
            _isEngineRunning = true;
        }

        public async Task StopAllAsync()
        {
            _logger.LogInformation("Stopping all hosted strategies...");
            foreach (var host in _hosts.Values)
            {
                await host.StopAsync();
            }
            _isEngineRunning = false;
        }

        public async Task PauseAllAsync()
        {
            _logger.LogInformation("Pausing all hosted strategies...");
            foreach (var host in _hosts.Values)
            {
                await host.PauseAsync();
            }
        }

        public async Task ResumeAllAsync()
        {
            _logger.LogInformation("Resuming all hosted strategies...");
            foreach (var host in _hosts.Values)
            {
                await host.ResumeAsync();
            }
        }

        public async Task RouteTickAsync(Tick tick, string correlationId)
        {
            if (!_isEngineRunning) return;

            foreach (var host in _hosts.Values)
            {
                await host.ProcessTickAsync(tick, correlationId);
            }
        }

        public async Task RouteBarAsync(Bar bar, string correlationId)
        {
            if (!_isEngineRunning) return;

            foreach (var host in _hosts.Values)
            {
                await host.ProcessBarAsync(bar, correlationId);
            }
        }
    }
}
