using System;

namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Configuration options for the high-performance diagnostic HTML logging system.
    /// </summary>
    public static class DiagnosticsOptions
    {
        /// <summary>
        /// Global runtime switch. When true, HTML logging is active.
        /// When false, the logging system becomes a near zero-cost no-op.
        /// </summary>
        public static bool EnableHtmlLogging { get; set; } = true;

        /// <summary>
        /// Maximum allowable log file size before trigger rotation (e.g. 10 MB).
        /// </summary>
        public static long MaxLogFileSizeMb { get; set; } = 10;

        /// <summary>
        /// Target folder where HTML reports are compiled.
        /// </summary>
        public static string LogDirectory { get; set; } = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusAI", "Logs");
    }
}