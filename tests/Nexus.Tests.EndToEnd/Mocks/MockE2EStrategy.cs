using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Tests.EndToEnd.Mocks
{
    public class MockE2EStrategy : IStrategy
    {
        public string Name { get; }
        public bool IsEnabled { get; private set; } = true;

        public int OnTickCount { get; private set; }
        public int OnBarCount { get; private set; }
        public Tick LastTick { get; private set; }

        private readonly Func<Tick, Task>? _onTickCallback;
        private bool _shouldThrow;

        public MockE2EStrategy(string name, Func<Tick, Task>? onTickCallback = null)
        {
            Name = name;
            _onTickCallback = onTickCallback;
        }

        public void ConfigureFault(bool shouldThrow)
        {
            _shouldThrow = shouldThrow;
        }

        public Task OnInitializeAsync()
        {
            IsEnabled = true;
            return Task.CompletedTask;
        }

        public async Task OnTickAsync(Tick tick)
        {
            if (_shouldThrow)
            {
                throw new InvalidOperationException($"Simulated strategy failure in {Name}");
            }

            OnTickCount++;
            LastTick = tick;

            if (_onTickCallback != null)
            {
                await _onTickCallback.Invoke(tick);
            }
        }

        public Task OnBarAsync(Bar bar)
        {
            OnBarCount++;
            return Task.CompletedTask;
        }

        public Task OnStopAsync()
        {
            IsEnabled = false;
            return Task.CompletedTask;
        }
    }
}
