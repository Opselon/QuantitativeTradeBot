using System;
using System.Collections.Generic;

namespace Nexus.Application.Dashboard
{
    public enum SystemHealthStatus
    {
        Healthy,
        Warning,
        Critical
    }

    public interface ISystemHealthMonitorService
    {
        SystemHealthStatus NativeEngineStatus { get; }
        SystemHealthStatus DecisionEngineStatus { get; }
        SystemHealthStatus MarketIntelligenceStatus { get; }
        SystemHealthStatus TrainingEngineStatus { get; }
        SystemHealthStatus ExecutionEngineStatus { get; }
        SystemHealthStatus DatabaseStatus { get; }
        SystemHealthStatus Mt5BridgeStatus { get; }

        double CpuUsage { get; }
        double MemoryUsageMb { get; }
        string ThreadPoolUtilization { get; }
        double TickProcessingLatencyMs { get; }
        double DecisionLatencyMs { get; }
        double ExecutionLatencyMs { get; }

        event Action<SystemHealthData>? OnHealthUpdated;

        void PushHealthUpdate(
            SystemHealthStatus native,
            SystemHealthStatus decision,
            SystemHealthStatus market,
            SystemHealthStatus training,
            SystemHealthStatus execution,
            SystemHealthStatus db,
            SystemHealthStatus bridge,
            double cpu,
            double memory,
            string threadPool,
            double tickLatency,
            double decisionLatency,
            double execLatency);
    }

    public class SystemHealthData
    {
        public SystemHealthStatus NativeEngineStatus { get; set; }
        public SystemHealthStatus DecisionEngineStatus { get; set; }
        public SystemHealthStatus MarketIntelligenceStatus { get; set; }
        public SystemHealthStatus TrainingEngineStatus { get; set; }
        public SystemHealthStatus ExecutionEngineStatus { get; set; }
        public SystemHealthStatus DatabaseStatus { get; set; }
        public SystemHealthStatus Mt5BridgeStatus { get; set; }

        public double CpuUsage { get; set; }
        public double MemoryUsageMb { get; set; }
        public string ThreadPoolUtilization { get; set; } = string.Empty;
        public double TickProcessingLatencyMs { get; set; }
        public double DecisionLatencyMs { get; set; }
        public double ExecutionLatencyMs { get; set; }
    }
}
