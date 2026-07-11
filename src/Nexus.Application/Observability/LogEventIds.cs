using Microsoft.Extensions.Logging;

namespace Nexus.Application.Observability
{
    public static class LogEventIds
    {
        public static readonly EventId MarketDataReceived = new(1001, "MarketDataReceived");
        public static readonly EventId ValidationRejected = new(1002, "ValidationRejected");

        public static readonly EventId StrategyStarted = new(2001, "StrategyStarted");
        public static readonly EventId StrategyStopped = new(2002, "StrategyStopped");
        public static readonly EventId StrategyFailed = new(2003, "StrategyFailed");

        public static readonly EventId SignalEmitted = new(3001, "SignalEmitted");
        public static readonly EventId RiskRejected = new(3002, "RiskRejected");

        public static readonly EventId OrderSubmitted = new(4001, "OrderSubmitted");
        public static readonly EventId OrderFilled = new(4002, "OrderFilled");
        public static readonly EventId OrderRejected = new(4003, "OrderRejected");

        public static readonly EventId RecoveryStarted = new(5001, "RecoveryStarted");
        public static readonly EventId RecoveryCompleted = new(5002, "RecoveryCompleted");

        public static readonly EventId NativeComputeInvoked = new(6001, "NativeComputeInvoked");
        public static readonly EventId NativeFallbackUsed = new(6002, "NativeFallbackUsed");

        public static readonly EventId WorkerStartup = new(7001, "WorkerStartup");
        public static readonly EventId WorkerShutdown = new(7002, "WorkerShutdown");
    }
}
