using System;

namespace Nexus.Application.Dashboard
{
    public sealed class SystemHealthMonitorService : ISystemHealthMonitorService
    {
        public SystemHealthStatus NativeEngineStatus { get; private set; } = SystemHealthStatus.Healthy;
        public SystemHealthStatus DecisionEngineStatus { get; private set; } = SystemHealthStatus.Healthy;
        public SystemHealthStatus MarketIntelligenceStatus { get; private set; } = SystemHealthStatus.Healthy;
        public SystemHealthStatus TrainingEngineStatus { get; private set; } = SystemHealthStatus.Healthy;
        public SystemHealthStatus ExecutionEngineStatus { get; private set; } = SystemHealthStatus.Healthy;
        public SystemHealthStatus DatabaseStatus { get; private set; } = SystemHealthStatus.Healthy;
        public SystemHealthStatus Mt5BridgeStatus { get; private set; } = SystemHealthStatus.Healthy;

        public double CpuUsage { get; private set; } = 4.2;
        public double MemoryUsageMb { get; private set; } = 42.5;
        public string ThreadPoolUtilization { get; private set; } = "12/250 Active Threads (0% Queue)";
        public double TickProcessingLatencyMs { get; private set; } = 0.005;
        public double DecisionLatencyMs { get; private set; } = 1.35;
        public double ExecutionLatencyMs { get; private set; } = 25.4;

        public event Action<SystemHealthData>? OnHealthUpdated;

        public void PushHealthUpdate(
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
            double execLatency)
        {
            NativeEngineStatus = native;
            DecisionEngineStatus = decision;
            MarketIntelligenceStatus = market;
            TrainingEngineStatus = training;
            ExecutionEngineStatus = execution;
            DatabaseStatus = db;
            Mt5BridgeStatus = bridge;

            CpuUsage = cpu;
            MemoryUsageMb = memory;
            ThreadPoolUtilization = threadPool;
            TickProcessingLatencyMs = tickLatency;
            DecisionLatencyMs = decisionLatency;
            ExecutionLatencyMs = execLatency;

            OnHealthUpdated?.Invoke(new SystemHealthData
            {
                NativeEngineStatus = native,
                DecisionEngineStatus = decision,
                MarketIntelligenceStatus = market,
                TrainingEngineStatus = training,
                ExecutionEngineStatus = execution,
                DatabaseStatus = db,
                Mt5BridgeStatus = bridge,
                CpuUsage = cpu,
                MemoryUsageMb = memory,
                ThreadPoolUtilization = threadPool,
                TickProcessingLatencyMs = tickLatency,
                DecisionLatencyMs = decisionLatency,
                ExecutionLatencyMs = execLatency
            });
        }
    }
}
