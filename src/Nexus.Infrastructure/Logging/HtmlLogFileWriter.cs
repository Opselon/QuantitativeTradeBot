using Nexus.Core.AI.Entities;
using System.Text;
using System.Threading.Channels;

namespace Nexus.Infrastructure.Logging
{
    public sealed class HtmlLogEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Severity { get; set; } = "INFO";
        public string Source { get; set; } = "System";
        public string ClassName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string InnerException { get; set; } = string.Empty;
        public int ThreadId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thread-safe, non-blocking background HTML log writer.
    /// Uses lock-free System.Threading.Channels to buffer logs for high-frequency trading.
    /// </summary>
    public sealed class HtmlLogFileWriter : IDisposable
    {
        #region Private Fields
        private readonly Channel<HtmlLogEvent> _logChannel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _writerTask;

        private string _currentFilePath = string.Empty;
        private DateTime _currentFileDate = DateTime.MinValue;
        private long _warningsCount = 0;
        private long _errorsCount = 0;
        #endregion

        #region Constructor
        public HtmlLogFileWriter()
        {
            // Configure high-performance, single-reader lock-free channel
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            };
            _logChannel = Channel.CreateUnbounded<HtmlLogEvent>(options);

            // Create target logs directory
            if (!Directory.Exists(DiagnosticsOptions.LogDirectory))
            {
                Directory.CreateDirectory(DiagnosticsOptions.LogDirectory);
            }

            // Launch long-running background thread to flush buffers asynchronously
            _writerTask = Task.Factory.StartNew(
                () => ProcessLogQueueAsync(_cts.Token),
                TaskCreationOptions.LongRunning);
        }
        #endregion

        #region Public API (Non-Blocking Enqueue)
        public void EnqueueLog(HtmlLogEvent logEvent)
        {
            if (!DiagnosticsOptions.EnableHtmlLogging) return;

            // Non-blocking try-write to channel
            _logChannel.Writer.TryWrite(logEvent);
        }
        #endregion

        #region Core Background Processing & Rotation
        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            var batch = new List<HtmlLogEvent>();
            var reader = _logChannel.Reader;

            while (await reader.WaitToReadAsync(token))
            {
                try
                {
                    // Read available logs from the queue channel
                    while (reader.TryRead(out var log))
                    {
                        batch.Add(log);
                    }

                    if (batch.Count > 0)
                    {
                        await FlushBatchToHtmlAsync(batch, token);
                        batch.Clear();
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HTML LOGGER FATAL] Processing queue failed: {ex.Message}");
                }
            }
        }

        private async Task FlushBatchToHtmlAsync(List<HtmlLogEvent> batch, CancellationToken token)
        {
            await RotateLogFileIfNeededAsync();

            var rowsBuilder = new StringBuilder();
            foreach (var log in batch)
            {
                // Increment counters
                if (log.Severity == "WARNING") Interlocked.Increment(ref _warningsCount);
                if (log.Severity == "ERROR" || log.Severity == "CRITICAL") Interlocked.Increment(ref _errorsCount);

                string rowClass = log.Severity == "WARNING" ? "warning-row" : (log.Severity == "ERROR" || log.Severity == "CRITICAL" ? "error-row" : "info-row");

                rowsBuilder.AppendLine($@"
                    <tr class='{rowClass}' data-severity='{log.Severity}'>
                        <td>{log.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}</td>
                        <td class='severity-badge'>{log.Severity}</td>
                        <td>{log.Source}</td>
                        <td>{log.ClassName}::{log.MethodName}</td>
                        <td>{log.Message}</td>
                        <td>{log.ThreadId}</td>
                        <td>{log.CorrelationId}</td>
                        <td>
                            {(!string.IsNullOrEmpty(log.StackTrace) ? $@"
                            <details>
                                <summary>{log.ExceptionType}</summary>
                                <pre>{log.StackTrace}</pre>
                                {(!string.IsNullOrEmpty(log.InnerException) ? $"<div class='inner-exc'>Inner: {log.InnerException}</div>" : "")}
                            </details>" : "None")}
                        </td>
                    </tr>");
            }

            // Read existing file and replace placeholder with newly buffered rows
            try
            {
                string htmlContent = await File.ReadAllTextAsync(_currentFilePath, token);

                // Update running counters in the sticky header dynamically
                htmlContent = htmlContent.Replace("id=\"warn-count\">0", $"id=\"warn-count\">{_warningsCount}");
                htmlContent = htmlContent.Replace("id=\"err-count\">0", $"id=\"err-count\">{_errorsCount}");

                // Insert rows at the top of the grid placeholder
                htmlContent = htmlContent.Replace("<!-- LOG_ROWS_PLACEHOLDER -->", rowsBuilder.ToString() + "\n<!-- LOG_ROWS_PLACEHOLDER -->");

                await File.WriteAllTextAsync(_currentFilePath, htmlContent, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTML LOGGER ERROR] Failed to write to file: {ex.Message}");
            }
        }

        private async Task RotateLogFileIfNeededAsync()
        {
            bool needsNewFile = false;

            if (string.IsNullOrEmpty(_currentFilePath) || _currentFileDate != DateTime.Today)
            {
                needsNewFile = true;
            }
            else
            {
                var fileInfo = new FileInfo(_currentFilePath);
                if (fileInfo.Length > DiagnosticsOptions.MaxLogFileSizeMb * 1024 * 1024)
                {
                    needsNewFile = true;
                }
            }

            if (needsNewFile)
            {
                _currentFileDate = DateTime.Today;
                string dateStr = _currentFileDate.ToString("yyyy-MM-dd");

                int index = 1;
                string filePath;
                do
                {
                    filePath = Path.Combine(DiagnosticsOptions.LogDirectory, $"nexus_observability_{dateStr}_{index:D3}.html");
                    index++;
                } while (File.Exists(filePath));

                _currentFilePath = filePath;
                _warningsCount = 0;
                _errorsCount = 0;

                await CreateNewHtmlLogFileAsync(_currentFilePath);
            }
        }

        private async Task CreateNewHtmlLogFileAsync(string filePath)
        {
            // Fully self-contained, modern dark-themed interactive HTML Log Report
            string template = $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Nexus Quant Workstation - Observability Report</title>
    <style>
        :root {{
            --bg-primary: #0F172A;
            --bg-card: #1E293B;
            --text-primary: #F1F5F9;
            --text-secondary: #94A3B8;
            --border: #334155;
            --primary: #3B82F6;
            --warning: #F59E0B;
            --error: #EF4444;
            --success: #10B981;
        }}
        body {{
            background-color: var(--bg-primary);
            color: var(--text-primary);
            font-family: 'Segoe UI', system-ui, sans-serif;
            margin: 0;
            padding: 0;
        }}
        header {{
            position: sticky;
            top: 0;
            background-color: var(--bg-card);
            border-bottom: 1px solid var(--border);
            padding: 15px 30px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            z-index: 1000;
        }}
        .brand {{
            font-size: 18px;
            font-weight: bold;
            color: var(--primary);
        }}
        .stats {{
            display: flex;
            gap: 20px;
        }}
        .stat-card {{
            background: #090D16;
            padding: 8px 15px;
            border-radius: 6px;
            border: 1px solid var(--border);
            font-size: 13px;
        }}
        .search-container {{
            padding: 20px 30px;
            display: flex;
            gap: 15px;
            background: #131B2E;
        }}
        input, select {{
            background: var(--bg-primary);
            border: 1px solid var(--border);
            color: var(--text-primary);
            padding: 10px 15px;
            border-radius: 6px;
            outline: none;
        }}
        input {{ width: 300px; }}
        table {{
            width: 100%;
            border-collapse: collapse;
            font-size: 13px;
        }}
        th, td {{
            padding: 12px 20px;
            text-align: left;
            border-bottom: 1px solid var(--border);
        }}
        th {{
            background: var(--bg-card);
            color: var(--text-secondary);
            position: sticky;
            top: 73px;
            z-index: 999;
        }}
        tr:hover {{ background: #1E293B50; }}
        .warning-row .severity-badge {{ color: var(--warning); font-weight: bold; }}
        .error-row .severity-badge {{ color: var(--error); font-weight: bold; }}
        .info-row .severity-badge {{ color: var(--success); }}
        details {{
            background: #0F172A;
            padding: 8px;
            border-radius: 4px;
            border: 1px solid var(--border);
        }}
        summary {{
            cursor: pointer;
            color: var(--primary);
            font-weight: bold;
        }}
        pre {{
            font-family: 'Consolas', monospace;
            font-size: 11px;
            overflow-x: auto;
            color: var(--text-secondary);
        }}
    </style>
</head>
<body>
    <header>
        <div class='brand'>NEXUS observABILITY DIAGNOSTICS REPORT</div>
        <div class='stats'>
            <div class='stat-card'>Warnings: <span id='warn-count' style='color: var(--warning); font-weight: bold;'>0</span></div>
            <div class='stat-card'>Errors: <span id='err-count' style='color: var(--error); font-weight: bold;'>0</span></div>
        </div>
    </header>

    <div class='search-container'>
        <input type='text' id='search' placeholder='Search logs (Source, Method, Message)...' onkeyup='filterLogs()'>
        <select id='severity-filter' onchange='filterLogs()'>
            <option value='ALL'>All Severities</option>
            <option value='INFO'>Info</option>
            <option value='WARNING'>Warning</option>
            <option value='ERROR'>Error</option>
        </select>
    </div>

    <table id='log-table'>
        <thead>
            <tr>
                <th style='width: 180px;'>Timestamp</th>
                <th style='width: 100px;'>Severity</th>
                <th style='width: 120px;'>Source</th>
                <th style='width: 200px;'>Class::Method</th>
                <th>Message</th>
                <th style='width: 80px;'>Thread ID</th>
                <th style='width: 120px;'>Correlation ID</th>
                <th>Exceptions</th>
            </tr>
        </thead>
        <tbody id='log-body'>
<!-- LOG_ROWS_PLACEHOLDER -->
        </tbody>
    </table>

    <script>
        function filterLogs() {{
            const searchVal = document.getElementById('search').value.toLowerCase();
            const severityVal = document.getElementById('severity-filter').value;
            const rows = document.getElementById('log-body').getElementsByTagName('tr');

            for (let row of rows) {{
                const text = row.innerText.toLowerCase();
                const severity = row.getAttribute('data-severity');

                const matchesSearch = text.includes(searchVal);
                const matchesSeverity = severityVal === 'ALL' || severity === severityVal || (severityVal === 'ERROR' && severity === 'CRITICAL');

                if (matchesSearch && matchesSeverity) {{
                    row.style.display = '';
                }} else {{
                    row.style.display = 'none';
                }}
            }}
        }}
    </script>
</body>
</html>";

            await File.WriteAllTextAsync(filePath, template, Encoding.UTF8);
        }
        #endregion

        #region Disposal
        public void Dispose()
        {
            _cts.Cancel();
            try { _writerTask.Wait(2000); } catch { }
            _cts.Dispose();
        }
        #endregion
    }
}