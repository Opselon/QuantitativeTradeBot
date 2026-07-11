using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexus.Application.Strategies
{
    public class InMemoryStrategyStateStore : IStrategyStateStore
    {
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _states = new();

        public Task SaveStateAsync(string strategyId, Dictionary<string, string> state)
        {
            _states[strategyId] = new Dictionary<string, string>(state);
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, string>?> LoadStateAsync(string strategyId)
        {
            if (_states.TryGetValue(strategyId, out var state))
            {
                return Task.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(state));
            }
            return Task.FromResult<Dictionary<string, string>?>(null);
        }
    }
}
