namespace Nexus.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration options for the database/persistence layers.
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// Gets or sets the target database provider (e.g. SQLite, PostgreSQL).
        /// </summary>
        public string Provider { get; set; } = "PostgreSQL";

        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        public string ConnectionString { get; set; } = "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";

        /// <summary>
        /// Gets or sets a value indicating whether TimescaleDB-style partitioned table features are enabled.
        /// </summary>
        public bool EnablePartitioning { get; set; } = true;
    }
}
