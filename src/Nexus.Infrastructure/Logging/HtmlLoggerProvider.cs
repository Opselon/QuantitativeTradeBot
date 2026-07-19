using Microsoft.Extensions.Logging;
using Nexus.Core.AI.Entities;
using System.Diagnostics;

namespace Nexus.Infrastructure.Logging
{
    /// <summary>
    /// Custom ILoggerProvider that registers and constructs instances of the high-performance HTML Logger.
    /// </summary>
    public sealed class HtmlLoggerProvider : ILoggerProvider
    {
        private readonly HtmlLogFileWriter _writer = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new HtmlLogger(categoryName, _writer);
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }

    /// <summary>
    /// Aspect-oriented HTML logger. Captures Microsoft Logging extensions calls
    /// and formats them into buffered lock-free HTML records.
    /// </summary>
    public sealed class HtmlLogger : ILogger
    {
        #region Private Fields
        private readonly string _categoryName;
        private readonly HtmlLogFileWriter _writer;
        #endregion

        #region Constructor
        public HtmlLogger(string categoryName, HtmlLogFileWriter writer)
        {
            _categoryName = categoryName ?? string.Empty;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }
        #endregion

        #region ILogger Implementations
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Zero-Cost Switch: Immediately exit if logging is disabled
            if (!DiagnosticsOptions.EnableHtmlLogging) return false;

            // Only log warnings, errors and critical crashes to keep high performance
            return logLevel >= LogLevel.Warning;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = formatter(state, exception);

            // Dynamically reconstruct callsite metadata via StackTrace diagnostics
            string className = _categoryName;
            string methodName = "Unknown";
            try
            {
                var stack = new StackTrace();
                for (int i = 0; i < stack.FrameCount; i++)
                {
                    var frame = stack.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method != null && method.DeclaringType != null &&
                        !method.DeclaringType.FullName!.StartsWith("Microsoft") &&
                        !method.DeclaringType.FullName!.StartsWith("System") &&
                        !method.DeclaringType.FullName!.StartsWith("Nexus.Infrastructure.Logging"))
                    {
                        className = method.DeclaringType.Name;
                        methodName = method.Name;
                        break;
                    }
                }
            }
            catch { }

            // REASON: Fixed to a stable static string to completely eliminate 
            // the compiled namespace conflict with WorkflowContext.Current in the infrastructure layer.
            string corrId = "SYSTEM";

            var logEvent = new HtmlLogEvent
            {
                Severity = logLevel.ToString().ToUpperInvariant(),
                Source = "Workstation",
                ClassName = className,
                MethodName = methodName,
                Message = message,
                ThreadId = Environment.CurrentManagedThreadId,
                CorrelationId = corrId,
                ExceptionType = exception?.GetType().Name ?? string.Empty,
                StackTrace = exception?.StackTrace ?? string.Empty,
                InnerException = exception?.InnerException?.Message ?? string.Empty
            };

            _writer.EnqueueLog(logEvent);
        }
        #endregion
    }
}