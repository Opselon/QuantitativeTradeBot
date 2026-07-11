using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Persistence
{
    public class SqliteDatabaseBootstrapper : IDatabaseBootstrapper
    {
        public string ProviderName => "SQLite";

        public async Task InitializeDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            // Create directory if it's a file database and directory doesn't exist
            var dataSource = ExtractDataSource(connectionString);
            if (!string.IsNullOrEmpty(dataSource) && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            var optionsBuilder = new DbContextOptionsBuilder<NexusDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            using var context = new NexusDbContext(optionsBuilder.Options);
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        public Task MigrateDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            // SQLite is initialized using EnsureCreated, so migration is a no-op or EnsureCreated
            return InitializeDatabaseAsync(connectionString, cancellationToken);
        }

        public async Task<bool> IsMigrationRequiredAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return true;

            var dataSource = ExtractDataSource(connectionString);
            if (!string.IsNullOrEmpty(dataSource) && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(dataSource))
                {
                    return true;
                }
            }

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NexusDbContext>();
                optionsBuilder.UseSqlite(connectionString);

                using var context = new NexusDbContext(optionsBuilder.Options);
                // Check if we can query the accounts table
                await context.Accounts.AnyAsync(cancellationToken);
                return false; // Table exists, we are good!
            }
            catch
            {
                return true; // Query failed, migration is required
            }
        }

        private static string ExtractDataSource(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var eq = part.IndexOf('=');
                if (eq >= 0)
                {
                    var key = part.Substring(0, eq).Trim();
                    var val = part.Substring(eq + 1).Trim();
                    if (key.Equals("Data Source", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("DataSource", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Filename", StringComparison.OrdinalIgnoreCase))
                    {
                        return val;
                    }
                }
            }
            return string.Empty;
        }
    }
}
