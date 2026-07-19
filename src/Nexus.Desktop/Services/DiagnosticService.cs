// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   PRESENTATION LAYER (Desktop UI Services)
// FILE:    DiagnosticService.cs
// REFERENCED BY: 
//   - src/Nexus.Desktop/ViewModels/MainViewModel.cs (Core Shell Logger)
//   - src/Nexus.Desktop/ViewModels/Workspaces/DiagnosticsViewModel.cs (DI Composition)
// DEPENDS ON:
//   - src/Nexus.Application/Observability/DiagnosticRingBuffer.cs (Central Log Target)
//   - src/Nexus.Application/Ports/BridgeDiagnosticLogEntry.cs (Data Type Reference)
// ============================================================================

using System;
using System.Collections.ObjectModel;
using Nexus.Application.Observability;
using Nexus.Application.Ports; // Reference to BridgeDiagnosticLogEntry port model

namespace Nexus.Desktop.Services
{
    /// <summary>
    /// Concrete implementation of the <see cref="IDiagnosticService"/> interface.
    /// Manages application-level logging and acts as a reactive unified conduit, 
    /// routing local diagnostic messages directly into the central high-performance <see cref="DiagnosticRingBuffer"/>.
    /// </summary>
    public class DiagnosticService : IDiagnosticService
    {
        /// <summary>
        /// The singleton reference to the central diagnostic ring buffer.
        /// Used to publish local system logs to the real-time UI logging pipelines.
        /// </summary>
        private readonly DiagnosticRingBuffer _ringBuffer;

        /// <summary>
        /// Local databound collection of <see cref="LogEntry"/> objects.
        /// Retained for backward-compatibility with older diagnostic views.
        /// </summary>
        private readonly ObservableCollection<LogEntry> _logs = new();

        /// <summary>
        /// Gets the local databound collection containing captured telemetry log entries.
        /// </summary>
        public ObservableCollection<LogEntry> Logs => _logs;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticService"/> class.
        /// Thread-safely resolves and integrates the central diagnostic ring buffer dependency.
        /// </summary>
        /// <param name="ringBuffer">The active singleton instance of the system diagnostic ring buffer.</param>
        /// <exception cref="ArgumentNullException">Thrown if the injected ring buffer is null.</exception>
        public DiagnosticService(DiagnosticRingBuffer ringBuffer)
        {
            // Guard clause to enforce Dependency Injection integrity across layers
            _ringBuffer = ringBuffer ?? throw new ArgumentNullException(nameof(ringBuffer));
        }

        /// <summary>
        /// Logs a structured system event, sanitizes its message, updates local UI collection,
        /// and automatically bridges the event to the centralized real-time <see cref="DiagnosticRingBuffer"/>.
        /// </summary>
        /// <param name="subsystem">The origin area of the system logging this event (e.g., AppShell, TradingDesk).</param>
        /// <param name="level">The severity tier of the event (e.g., INFO, WARN, ERROR).</param>
        /// <param name="message">The raw, unmodified text description of the event.</param>
        public void Log(string subsystem, string level, string message)
        {
            // Sanitize input message payload to protect against sensitive credential leaks or invalid characters
            string sanitizedMessage = LogSanitizer.Sanitize(message);

            // Ensure the local collection is modified safely on the main GUI thread of the WPF application context
            if (System.Windows.Application.Current != null)
            {
                // Asynchronously dispatch the addition of the log item to the UI thread
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Append new LogEntry object to the local tracking collection
                    _logs.Add(new LogEntry
                    {
                        // Record localized wall-clock current timestamp
                        Timestamp = DateTime.Now,
                        // Assign origin subsystem category name
                        Subsystem = subsystem,
                        // Set severity level
                        Level = level,
                        // Store the sanitized message string
                        Message = sanitizedMessage
                    });

                    // Cap the maximum capacity of the local backward-compatibility collection
                    if (_logs.Count > 200)
                    {
                        // Remove oldest diagnostic trace from the top index
                        _logs.RemoveAt(0);
                    }
                }));
            }
            else
            {
                // Fallback for isolated headless environments, test suites, or background processes
                _logs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Subsystem = subsystem,
                    Level = level,
                    Message = sanitizedMessage
                });

                // Enforce maximum capacity constraint in headless execution state
                if (_logs.Count > 200)
                {
                    _logs.RemoveAt(0);
                }
            }

            // CRITICAL BRIDGE CONDUIT: Transform and publish this system diagnostic log to the central ring buffer
            // This triggers the DiagnosticRingBuffer.EntryAdded event instantly, making it show up in DiagnosticsView.
            _ringBuffer.Add(new BridgeDiagnosticLogEntry
            {
                // Standardized UTC timestamp required by the database and core logging systems
                TimestampUtc = DateTime.UtcNow,
                // Map local subsystem to Category column
                Category = subsystem,
                // Map log level to Severity column
                Severity = level,
                // Label as "SYSTEM" to differentiate local platform traces from broker message exchanges
                Direction = "SYSTEM",
                // Store sanitized diagnostic payload
                Message = sanitizedMessage
            });
        }
    }
}