using System.Collections.ObjectModel;

namespace Nexus.Desktop.Services
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Level { get; set; } = "INFO"; // INFO, WARN, ERROR, DEBUG
        public string Subsystem { get; set; } = "System";
        public string Message { get; set; } = string.Empty;
    }

    public interface IDiagnosticService
    {
        ObservableCollection<LogEntry> Logs { get; }
        void Log(string subsystem, string level, string message);
    }
}
