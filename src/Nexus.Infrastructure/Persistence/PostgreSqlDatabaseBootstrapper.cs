using Microsoft.EntityFrameworkCore;
using Nexus.Application.Ports;
using Npgsql;

namespace Nexus.Infrastructure.Persistence
{
    public class PostgreSqlDatabaseBootstrapper : IDatabaseBootstrapper
    {
        public string ProviderName => "PostgreSQL";

        public async Task InitializeDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            // Ensure database is created (Postgres might require connecting to postgres database to CREATE DATABASE first if it doesn't exist,
            // but usually we can try to connect and run scripts, or use EF's context database creation)
            var optionsBuilder = new DbContextOptionsBuilder<NexusDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            using var context = new NexusDbContext(optionsBuilder.Options);

            // To ensure the DB itself exists on the server, we can call EnsureCreated or use EF Core Migrations.
            // Since NTE has raw SQL script partitions, we can run them directly on the connection!
            var scriptsDir = GetScriptsDirectory();
            var script001 = await File.ReadAllTextAsync(Path.Combine(scriptsDir, "001_create_schema.sql"), cancellationToken);
            var script002 = await File.ReadAllTextAsync(Path.Combine(scriptsDir, "002_create_market_partitions.sql"), cancellationToken);
            var script003 = await File.ReadAllTextAsync(Path.Combine(scriptsDir, "003_create_indexes.sql"), cancellationToken);

            await ExecuteRawSqlAsync(connectionString, script001, cancellationToken);
            await ExecuteRawSqlAsync(connectionString, script002, cancellationToken);
            await ExecuteRawSqlAsync(connectionString, script003, cancellationToken);
        }

        public async Task MigrateDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            // Apply standard EF migrations
            var optionsBuilder = new DbContextOptionsBuilder<NexusDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            using var context = new NexusDbContext(optionsBuilder.Options);
            await context.Database.MigrateAsync(cancellationToken);
        }

        public async Task<bool> IsMigrationRequiredAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return true;

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NexusDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var context = new NexusDbContext(optionsBuilder.Options);
                // Check if we can query the accounts table
                await context.Accounts.AnyAsync(cancellationToken);
                return false; // Table exists, we are good!
            }
            catch
            {
                return true; // Query failed, migration/init is required
            }
        }

        private static string GetScriptsDirectory()
        {
            var currentDir = AppContext.BaseDirectory;
            while (currentDir != null)
            {
                var potentialPath = Path.Combine(currentDir, "src", "Nexus.Infrastructure", "Persistence", "Scripts");
                if (Directory.Exists(potentialPath))
                {
                    return potentialPath;
                }
                potentialPath = Path.Combine(currentDir, "Persistence", "Scripts");
                if (Directory.Exists(potentialPath))
                {
                    return potentialPath;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            throw new DirectoryNotFoundException("Could not find the SQL scripts directory.");
        }

        private static async Task ExecuteRawSqlAsync(string connectionString, string sql, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
