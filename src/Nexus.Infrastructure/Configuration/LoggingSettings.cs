namespace Nexus.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration options for system logging and observability diagnostics.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Gets or sets the default minimum log level (e.g. Debug, Information, Warning, Error).
        /// </summary>
        public string DefaultLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets the target file path for persisting system log files.
        /// </summary>
        public string LogFilePath { get; set; } = "logs/nexus_trading.log";

        /// <summary>
        /// Gets or sets a value indicating whether standard console logging is enabled.
        /// </summary>
        public bool EnableConsole { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether flat file logging is enabled.
        /// </summary>
        public bool EnableFile { get; set; } = true;
    }
}
