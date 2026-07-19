using Microsoft.Extensions.Logging;

namespace Nexus.Infrastructure.Logging
{
    /// <summary>
    /// Installs system-wide unhandled exception hooks for the .NET Runtime.
    /// Captures background tasks and AppDomain failures automatically.
    /// </summary>
    public static class AppDomainExceptionHook
    {
        private static ILogger? _logger;

        public static void Register(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 1. AppDomain Unhandled Crashes
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 2. Task Scheduler Unobserved Exceptions (Async background tasks)
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _logger?.LogCritical(ex, "[FATAL APPDOMAIN CRASH] Unhandled exception intercepted.");
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "[UNOBSERVED TASK FAULT] Background task exception intercepted.");
            e.SetObserved();
        }
    }
}