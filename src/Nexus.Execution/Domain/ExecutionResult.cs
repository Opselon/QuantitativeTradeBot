using System;

namespace Nexus.Execution.Domain
{
    public class ExecutionResult
    {
        public bool Success { get; }
        public string OrderId { get; }
        public string Error { get; }
        public double ExecutionTime { get; } // latency/time in milliseconds or seconds

        public ExecutionResult(bool success, string orderId, string error, double executionTime)
        {
            Success = success;
            OrderId = orderId ?? string.Empty;
            Error = error ?? string.Empty;
            ExecutionTime = executionTime;
        }

        public static ExecutionResult Succeeded(string orderId, double executionTime) =>
            new ExecutionResult(true, orderId, string.Empty, executionTime);

        public static ExecutionResult Failed(string error, double executionTime) =>
            new ExecutionResult(false, string.Empty, error, executionTime);
    }
}
