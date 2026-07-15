using System.Data.Common;

namespace Nexus.Application.Ports
{
    /// <summary>
    /// Port interface responsible for establishing database connections in an abstracted manner.
    /// This keeps database-specific driver types out of high-level application flows.
    /// </summary>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Creates and returns a new database connection instance.
        /// </summary>
        /// <returns>A configured <see cref="DbConnection"/>.</returns>
        DbConnection CreateConnection();
    }
}
