// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   PRESENTATION LAYER (WPF Desktop ViewModels)
// FILE:    DiagnosticsViewModel.cs
// REFERENCED BY: 
//   - src/Nexus.Desktop/Views/Workspaces/DiagnosticsView.xaml (DataContext Binding)
// DEPENDS ON:
//   - src/Nexus.Application/Observability/DiagnosticRingBuffer.cs (Data Publisher)
//   - src/Nexus.Desktop/Services/IDiagnosticService.cs (Internal Application Logger)
// ============================================================================

using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Nexus.Application.Observability;
using Nexus.Application.Ports;
using Nexus.Desktop.Services;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    /// <summary>
    /// Core ViewModel driving the Live Diagnostics Workspace View.
    /// Manages the data bindings, thread-marshalled reactive log streams, and safe dispose lifecycles.
    /// </summary>
    public class DiagnosticsViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Reference to the global, memory-bounded, thread-safe application diagnostic ring buffer.
        /// Backing source for Mt5 bridge logs and real-time execution anomalies.
        /// </summary>
        private readonly DiagnosticRingBuffer _ringBuffer;

        /// <summary>
        /// Reference to the internal application logging service designed for subsystem telemetry.
        /// </summary>
        private readonly IDiagnosticService _diagnosticService;

        /// <summary>
        /// Tracks whether the dispose sequence has been triggered for this instance.
        /// Prevents duplicate resource disposal runs and object access exceptions.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Exposes the referenced internal diagnostic service to the view-binding tree if required.
        /// </summary>
        public IDiagnosticService DiagnosticService => _diagnosticService;

        /// <summary>
        /// Databound collection populated with structured logs.
        /// Must be updated only on the main WPF Dispatcher GUI Thread to prevent cross-thread violations.
        /// </summary>
        public ObservableCollection<BridgeDiagnosticLogEntry> StructuredLogs { get; } = new();

        /// <summary>
        /// Command to trigger a manual refresh on the diagnostic logs collection from the source ring buffer.
        /// </summary>
        public ICommand RefreshLogsCommand { get; }

        /// <summary>
        /// Command to wipe the backing buffer and empty the on-screen logs list.
        /// </summary>
        public ICommand ClearLogsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsViewModel"/> class with required infrastructure dependencies.
        /// </summary>
        /// <param name="ringBuffer">The global system diagnostic logging buffer.</param>
        /// <param name="diagnosticService">The system logging manager for core desktop client events.</param>
        public DiagnosticsViewModel(DiagnosticRingBuffer ringBuffer, IDiagnosticService diagnosticService)
        {
            // Guard clause to enforce strict Dependency Injection compliance and avoid null crashes
            _ringBuffer = ringBuffer ?? throw new ArgumentNullException(nameof(ringBuffer));

            // Validate and capture the application-level diagnostic logger dependency
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));

            // Map UI interaction to the appropriate execution routine for Refreshing logs
            RefreshLogsCommand = new RelayCommand(OnRefreshLogs);

            // Map UI interaction to the appropriate execution routine for Clearing logs
            ClearLogsCommand = new RelayCommand(OnClearLogs);

            // Register subscriber event handler to capture new logs instantly as they get queued
            _ringBuffer.EntryAdded += OnEntryAdded;

            // Trigger initial data load to display any existing cached records from active session
            OnRefreshLogs();
        }

        /// <summary>
        /// Event listener that processes incoming real-time logs from the backing ring buffer.
        /// Safely marshals updates onto the WPF graphic dispatcher thread.
        /// </summary>
        /// <param name="sender">The sender object invoking the event.</param>
        /// <param name="entry">The newly appended diagnostic log record.</param>
        private void OnEntryAdded(object? sender, BridgeDiagnosticLogEntry entry)
        {
            // Avoid processing invalid empty events
            if (entry == null) return;

            // Check if the current context has an active WPF Application environment
            if (System.Windows.Application.Current != null)
            {
                // Marshal execution onto the UI Thread asynchronously (BeginInvoke is non-blocking)
                // This prevents high-frequency bridge messages from lagging/locking the rendering flow
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Add the freshly received log element to the bound UI collection
                    StructuredLogs.Add(entry);

                    // Bound-check the UI list size to prevent layout and memory drag over days of active run
                    if (StructuredLogs.Count > 500)
                    {
                        // Remove the oldest visible log item from the top of the grid
                        StructuredLogs.RemoveAt(0);
                    }
                }));
            }
            else
            {
                // Fallback for non-WPF environments, design times, or unit testing pipelines
                StructuredLogs.Add(entry);

                // Maintain stable bounded size limit
                if (StructuredLogs.Count > 500)
                {
                    StructuredLogs.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Forcefully clears and synchronizes the active databound collection from the backing buffer storage.
        /// </summary>
        private void OnRefreshLogs()
        {
            // Verify if WPF context is available to ensure UI Thread alignment
            if (System.Windows.Application.Current != null)
            {
                // Synchronously invoke on the UI dispatcher to prevent threading race conditions
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Clean slate on the current screen log grid
                    StructuredLogs.Clear();

                    // Fetch the latest 100 historical records currently stored in the ring buffer
                    var logs = _ringBuffer.Query(limit: 100);

                    // Loop through the queried records
                    foreach (var log in logs)
                    {
                        // Safely add each record into the bound UI collection
                        StructuredLogs.Add(log);
                    }
                });
            }
            else
            {
                // Execution routine for headless environments, scripts or unit tests
                StructuredLogs.Clear();
                var logs = _ringBuffer.Query(limit: 100);
                foreach (var log in logs)
                {
                    StructuredLogs.Add(log);
                }
            }
        }

        /// <summary>
        /// Performs full purge sequence clearing both the core application buffer and the WPF screen.
        /// </summary>
        private void OnClearLogs()
        {
            // Wipe the primary ring buffer storage from memory completely
            _ringBuffer.Clear();

            // Check if WPF application instance is active
            if (System.Windows.Application.Current != null)
            {
                // Synchronously clear the UI list on the main graphics thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Instantly empty out on-screen visual rows
                    StructuredLogs.Clear();
                });
            }
            else
            {
                // Direct cleanup fallback
                StructuredLogs.Clear();
            }
        }

        /// <summary>
        /// Public implementation of IDisposable.Dispose. Initiates clean-up.
        /// </summary>
        public void Dispose()
        {
            // Call virtual dispose method with true to trigger managed resource cleanup
            Dispose(true);

            // Tell the garbage collector that the object's finalizer does not need to run
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs virtual, thread-safe resource disposal of managed and unmanaged dependencies.
        /// Prevents subscriber memory leakages from orphaned EventHandlers.
        /// </summary>
        /// <param name="disposing">True if called from public Dispose, false if called from GC Finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Guard clause to execute cleanup only once
            if (!_disposed)
            {
                // If disposing of managed objects
                if (disposing)
                {
                    // Check if the source ring buffer instance exists
                    if (_ringBuffer != null)
                    {
                        // CRITICAL: Unsubscribe the handler to allow GC to clean up this view model instance.
                        // Without this, the publisher (RingBuffer) retains a strong reference, causing a memory leak.
                        _ringBuffer.EntryAdded -= OnEntryAdded;
                    }
                }

                // Toggle flag to indicate cleanup completion
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer for the <see cref="DiagnosticsViewModel"/> class.
        /// Serves as a failsafe to guarantee clean closure of system resources if Dispose was not invoked.
        /// </summary>
        ~DiagnosticsViewModel()
        {
            // Trigger disposal with disposing set to false
            Dispose(false);
        }
    }
}