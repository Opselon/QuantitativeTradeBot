using System;

namespace Nexus.Application.Ports
{
    public interface IConnectionHealthMonitor : IDisposable
    {
        bool IsHealthy { get; }
        event Action<bool>? OnHealthChanged;
        void StartMonitoring(IMt5Session session);
        void StopMonitoring();
    }
}
