using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Npgsql;
using Nexus.Application.Ports;
using Nexus.Infrastructure.Configuration;

namespace Nexus.Infrastructure.Persistence
{
    /// <summary>
    /// Factory for establishing database connection instances.
    /// Safely resolves the active configured database driver engine using configured Options.
    /// </summary>
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly DatabaseSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionFactory"/> class.
        /// </summary>
        /// <param name="options">The registered database settings options.</param>
        public ConnectionFactory(IOptions<DatabaseSettings> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _settings = options.Value ?? throw new ArgumentException("Database settings cannot be null.", nameof(options));
        }

        /// <inheritdoc />
        public DbConnection CreateConnection()
        {
            var provider = _settings.Provider ?? "PostgreSQL";
            var connString = _settings.ConnectionString;

            if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                return new SqliteConnection(connString);
            }
            else
            {
                return new NpgsqlConnection(connString);
            }
        }
    }
}
