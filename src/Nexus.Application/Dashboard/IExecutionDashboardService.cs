using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexus.Application.Dashboard
{
    public enum ExecutionDashboardProfile
    {
        Simulation,
        Paper,
        Live
    }

    public interface IExecutionDashboardService
    {
        ExecutionDashboardProfile CurrentProfile { get; }
        bool IsLivePermissionGranted { get; }
        IReadOnlyList<string> PermissionAuditLog { get; }

        event Action<ExecutionDashboardData>? OnExecutionUpdated;

        void SetProfile(ExecutionDashboardProfile profile);
        Task<bool> RequestToggleLivePermissionAsync(bool enable, Func<string, Task<bool>> confirmCallback);
        void PushExecutionUpdate(double balance, double equity, double margin, double exposure, double drawdown, int openPositionsCount);
        void AddAuditLogEntry(string message);
    }

    public class ExecutionDashboardData
    {
        public ExecutionDashboardProfile Profile { get; set; }
        public bool IsLivePermissionGranted { get; set; }
        public double Balance { get; set; }
        public double Equity { get; set; }
        public double Margin { get; set; }
        public double Exposure { get; set; }
        public double Drawdown { get; set; }
        public int OpenPositionsCount { get; set; }
    }
}
