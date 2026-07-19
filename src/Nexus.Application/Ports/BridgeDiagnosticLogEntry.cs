namespace Nexus.Application.Ports
{
    public class BridgeDiagnosticLogEntry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Severity { get; set; } = "INFO"; // TRACE, DEBUG, INFO, WARN, ERROR, CRITICAL
        public string Category { get; set; } = "Bridge"; // Bridge, Login, Network, MarketData, NativeCore, SmokeTest
        public string Direction { get; set; } = "Internal"; // Inbound, Outbound, Internal
        public string Message { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public double DurationMs { get; set; }
        public string PayloadSummary { get; set; } = string.Empty;
        public string ExceptionSummary { get; set; } = string.Empty;
    }
}
