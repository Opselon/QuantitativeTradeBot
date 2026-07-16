using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexus.Application.Dashboard
{
    public sealed class ExecutionDashboardService : IExecutionDashboardService
    {
        public ExecutionDashboardProfile CurrentProfile { get; private set; } = ExecutionDashboardProfile.Simulation;
        public bool IsLivePermissionGranted { get; private set; } = false;

        private readonly List<string> _permissionAuditLog = new();
        public IReadOnlyList<string> PermissionAuditLog => _permissionAuditLog;

        public event Action<ExecutionDashboardData>? OnExecutionUpdated;

        private double _balance = 100000.0;
        private double _equity = 100000.0;
        private double _margin = 0.0;
        private double _exposure = 0.0;
        private double _drawdown = 0.0;
        private int _openPositionsCount = 0;

        public ExecutionDashboardService()
        {
            AddAuditLogEntry("Execution system initialized in Simulation mode. Live trading disabled.");
        }

        public void SetProfile(ExecutionDashboardProfile profile)
        {
            if (CurrentProfile == profile) return;

            string oldProfile = CurrentProfile.ToString();
            CurrentProfile = profile;

            AddAuditLogEntry($"Execution profile changed from {oldProfile} to {profile}.");

            // Auto-disable live permission if profile changes away from Live
            if (profile != ExecutionDashboardProfile.Live && IsLivePermissionGranted)
            {
                IsLivePermissionGranted = false;
                AddAuditLogEntry("Live trading permission automatically revoked due to profile switch.");
            }

            TriggerUpdate();
        }

        public async Task<bool> RequestToggleLivePermissionAsync(bool enable, Func<string, Task<bool>> confirmCallback)
        {
            if (enable == IsLivePermissionGranted) return IsLivePermissionGranted;

            if (enable)
            {
                if (CurrentProfile != ExecutionDashboardProfile.Live)
                {
                    AddAuditLogEntry("REJECTED: Cannot enable Live permission when profile is not set to Live.");
                    return false;
                }

                // Explicit security dialog box or confirmation prompt required
                string prompt = "WARNING: You are enabling LIVE execution permission. Real money orders will be routed directly to MetaTrader 5 live terminal. Do you want to continue?";
                bool confirmed = await confirmCallback(prompt);

                if (confirmed)
                {
                    IsLivePermissionGranted = true;
                    AddAuditLogEntry("SECURITY PERMISSION GRANTED: Live trading permission enabled by explicit user confirmation.");
                    TriggerUpdate();
                    return true;
                }
                else
                {
                    AddAuditLogEntry("SECURITY PERMISSION REJECTED: User aborted live permission activation prompt.");
                    return false;
                }
            }
            else
            {
                IsLivePermissionGranted = false;
                AddAuditLogEntry("SECURITY PERMISSION REVOKED: Live trading permission deactivated cleanly.");
                TriggerUpdate();
                return true;
            }
        }

        public void PushExecutionUpdate(double balance, double equity, double margin, double exposure, double drawdown, int openPositionsCount)
        {
            _balance = balance;
            _equity = equity;
            _margin = margin;
            _exposure = exposure;
            _drawdown = drawdown;
            _openPositionsCount = openPositionsCount;

            TriggerUpdate();
        }

        public void AddAuditLogEntry(string message)
        {
            string log = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] {message}";
            lock (_permissionAuditLog)
            {
                _permissionAuditLog.Add(log);
            }
        }

        private void TriggerUpdate()
        {
            OnExecutionUpdated?.Invoke(new ExecutionDashboardData
            {
                Profile = CurrentProfile,
                IsLivePermissionGranted = IsLivePermissionGranted,
                Balance = _balance,
                Equity = _equity,
                Margin = _margin,
                Exposure = _exposure,
                Drawdown = _drawdown,
                OpenPositionsCount = _openPositionsCount
            });
        }
    }
}
