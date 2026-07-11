using System;
using System.Collections.ObjectModel;
using Nexus.Application.Observability;

namespace Nexus.Desktop.Services
{
    public class DiagnosticService : IDiagnosticService
    {
        public ObservableCollection<LogEntry> Logs { get; } = new();

        public void Log(string subsystem, string level, string message)
        {
            var sanitizedMessage = LogSanitizer.Sanitize(message);

            // Ensure we update collection on the UI thread
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Logs.Add(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Subsystem = subsystem,
                        Level = level,
                        Message = sanitizedMessage
                    });

                    // Limit log list size to prevent memory buildup
                    if (Logs.Count > 200)
                    {
                        Logs.RemoveAt(0);
                    }
                }));
            }
            else
            {
                Logs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Subsystem = subsystem,
                    Level = level,
                    Message = sanitizedMessage
                });
            }
        }
    }
}
