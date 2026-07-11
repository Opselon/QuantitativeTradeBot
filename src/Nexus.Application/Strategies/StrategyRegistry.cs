using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Strategies
{
    public class StrategyRegistry : IStrategyRegistry
    {
        private readonly ConcurrentDictionary<string, (StrategyDescriptor Descriptor, IStrategy Strategy)> _registry = new();

        public void Register(StrategyDescriptor descriptor, IStrategy strategy)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            _registry[descriptor.StrategyId] = (descriptor, strategy);
        }

        public IReadOnlyList<StrategyDescriptor> GetDescriptors()
        {
            return _registry.Values.Select(v => v.Descriptor).ToList();
        }

        public IStrategy? GetStrategy(string strategyId)
        {
            return _registry.TryGetValue(strategyId, out var pair) ? pair.Strategy : null;
        }

        public StrategyDescriptor? GetDescriptor(string strategyId)
        {
            return _registry.TryGetValue(strategyId, out var pair) ? pair.Descriptor : null;
        }
    }
}
