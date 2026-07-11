using System;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedConnectionHealthMonitor : IConnectionHealthMonitor
    {
        private IMt5Session? _session;
        public bool IsHealthy { get; private set; } = true;

        public event Action<bool>? OnHealthChanged;

        public void StartMonitoring(IMt5Session session)
        {
            _session = session;
            _session.OnStatusChanged += HandleStatusChanged;
            UpdateHealth();
        }

        public void StopMonitoring()
        {
            if (_session != null)
            {
                _session.OnStatusChanged -= HandleStatusChanged;
                _session = null;
            }
        }

        private void HandleStatusChanged(GatewayConnectionStatus status)
        {
            UpdateHealth();
        }

        private void UpdateHealth()
        {
            var wasHealthy = IsHealthy;
            IsHealthy = _session != null && _session.Status == GatewayConnectionStatus.Connected;
            if (wasHealthy != IsHealthy)
            {
                OnHealthChanged?.Invoke(IsHealthy);
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}
