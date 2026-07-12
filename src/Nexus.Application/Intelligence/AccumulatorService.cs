using System;
using System.Collections.Concurrent;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Intelligence
{
    public class AccumulatorService : IAccumulatorService
    {
        private readonly ConcurrentDictionary<string, AccumulatorState> _states = new(StringComparer.OrdinalIgnoreCase);

        public AccumulatorState GetState(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or whitespace.", nameof(symbol));

            return _states.GetOrAdd(symbol, s => new AccumulatorState(s));
        }

        public AccumulatorState UpdateState(FeatureDelta delta)
        {
            var state = GetState(delta.Symbol);
            lock (state)
            {
                state.ApplyDelta(delta);
            }
            return state;
        }

        public void ResetState(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return;
            _states.TryRemove(symbol, out _);
        }
    }
}
