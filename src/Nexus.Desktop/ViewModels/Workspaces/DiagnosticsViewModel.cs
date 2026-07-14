using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Nexus.Application.Observability;
using Nexus.Application.Ports;
using Nexus.Desktop.Services;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class DiagnosticsViewModel : ViewModelBase
    {
        private readonly DiagnosticRingBuffer _ringBuffer;
        private readonly IDiagnosticService _diagnosticService;

        public IDiagnosticService DiagnosticService => _diagnosticService;

        public ObservableCollection<BridgeDiagnosticLogEntry> StructuredLogs { get; } = new();

        public ICommand RefreshLogsCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public DiagnosticsViewModel(DiagnosticRingBuffer ringBuffer, IDiagnosticService diagnosticService)
        {
            _ringBuffer = ringBuffer;
            _diagnosticService = diagnosticService;

            RefreshLogsCommand = new RelayCommand(OnRefreshLogs);
            ClearLogsCommand = new RelayCommand(OnClearLogs);

            OnRefreshLogs();
        }

        private void OnRefreshLogs()
        {
            StructuredLogs.Clear();
            var logs = _ringBuffer.Query(limit: 100);
            foreach (var log in logs)
            {
                StructuredLogs.Add(log);
            }
        }

        private void OnClearLogs()
        {
            _ringBuffer.Clear();
            StructuredLogs.Clear();
        }
    }
}
