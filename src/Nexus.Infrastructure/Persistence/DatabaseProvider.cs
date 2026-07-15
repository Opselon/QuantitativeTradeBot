using System;
using Microsoft.Extensions.Options;
using Nexus.Application.Ports;
using Nexus.Infrastructure.Configuration;

namespace Nexus.Infrastructure.Persistence
{
    /// <summary>
    /// Database provider metadata service.
    /// Uses registered <see cref="DatabaseSettings"/> Options to query the active relational engine context.
    /// </summary>
    public class DatabaseProvider : IDatabaseProvider
    {
        private readonly DatabaseSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider"/> class.
        /// </summary>
        /// <param name="options">The registered database settings options.</param>
        public DatabaseProvider(IOptions<DatabaseSettings> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _settings = options.Value ?? throw new ArgumentException("Database settings cannot be null.", nameof(options));
        }

        /// <inheritdoc />
        public string ProviderName => _settings.Provider ?? "PostgreSQL";

        /// <inheritdoc />
        public string ConnectionString => _settings.ConnectionString;

        /// <inheritdoc />
        public bool SupportsPartitioning => !ProviderName.Equals("SQLite", StringComparison.OrdinalIgnoreCase);
    }
}
