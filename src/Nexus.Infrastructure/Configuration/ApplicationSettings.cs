namespace Nexus.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration options for the general trading system engine environment.
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Gets or sets the execution context environment (e.g. Development, Testing, Production).
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Gets or sets the application name identifier.
        /// </summary>
        public string AppName { get; set; } = "Nexus Trading Engine";

        /// <summary>
        /// Gets or sets a value indicating whether live trading (non-simulated execution) is enabled.
        /// Defaults to false for fail-safe protection.
        /// </summary>
        public bool IsLiveTradingEnabled { get; set; } = false;
    }
}
