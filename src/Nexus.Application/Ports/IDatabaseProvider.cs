namespace Nexus.Application.Ports
{
    /// <summary>
    /// Port interface representing the abstracted database provider in use.
    /// Allows the application to query details about the active persistence engine.
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Gets the name of the active database provider (e.g. SQLite, PostgreSQL).
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the active connection string.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Gets a value indicating whether the current provider supports timeseries partitioning.
        /// </summary>
        bool SupportsPartitioning { get; }
    }
}
