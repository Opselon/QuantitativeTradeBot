using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexus.Infrastructure.Configuration;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Persistence.Models;
using Nexus.Infrastructure.Persistence.Repositories;
using Nexus.Infrastructure.Storage.FileStorage;
using System.IO;
using System.Text;

namespace Nexus.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Comprehensive Unit tests validating infrastructure base models, generic repository behavior,
    /// database providers, configuration settings, and local file storage operations.
    /// </summary>
    public class InfrastructureTest : IDisposable
    {
        private readonly string _tempStorageDir;

        /// <summary>
        /// Initializes test state and creates a safe temporary storage folder.
        /// </summary>
        public InfrastructureTest()
        {
            _tempStorageDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nexus_test_storage_" + Guid.NewGuid()));
        }

        /// <summary>
        /// Cleans up any files or folders created during test execution.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(_tempStorageDir))
            {
                Directory.Delete(_tempStorageDir, recursive: true);
            }
        }

        #region LocalFileStorage Tests
        [Fact]
        public async Task LocalFileStorage_ShouldSaveAndLoadBytesCorrectly()
        {
            // Arrange
            var storage = new LocalFileStorage(_tempStorageDir);
            var content = Encoding.UTF8.GetBytes("Hello Quantitative Trader!");
            var path = "models/nexus_v1_0.bin";

            // Act
            await storage.SaveFileBytesAsync(path, content);
            var exists = await storage.FileExistsAsync(path);
            var loaded = await storage.LoadFileBytesAsync(path);

            // Assert
            Assert.True(exists);
            Assert.Equal("Hello Quantitative Trader!", Encoding.UTF8.GetString(loaded));

            // Delete
            await storage.DeleteFileAsync(path);
            var existsAfterDelete = await storage.FileExistsAsync(path);
            Assert.False(existsAfterDelete);
        }

        [Fact]
        public async Task LocalFileStorage_ShouldThrowOnPathTraversal()
        {
            // Arrange
            var storage = new LocalFileStorage(_tempStorageDir);
            var content = Encoding.UTF8.GetBytes("Traverse!");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                storage.SaveFileBytesAsync("../dangerous.bin", content));
        }

        [Fact]
        public async Task LocalFileStorage_ShouldThrowOnAbsoluteRootedPath()
        {
            // Arrange
            var storage = new LocalFileStorage(_tempStorageDir);
            var content = Encoding.UTF8.GetBytes("Absolute!");
            var absolutePath = "/etc/passwd";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                storage.SaveFileBytesAsync(absolutePath, content));
        }
        #endregion

        #region DatabaseProvider & ConnectionFactory Tests
        [Fact]
        public void DatabaseProvider_ShouldReflectConfigSettings()
        {
            // Arrange
            var dbSettings = new DatabaseSettings
            {
                Provider = "SQLite",
                ConnectionString = "Data Source=:memory:"
            };
            var options = Options.Create(dbSettings);

            var provider = new DatabaseProvider(options);

            // Assert
            Assert.Equal("SQLite", provider.ProviderName);
            Assert.Equal("Data Source=:memory:", provider.ConnectionString);
            Assert.False(provider.SupportsPartitioning);
        }

        [Fact]
        public void ConnectionFactory_ShouldCreateSqliteConnection_WhenConfigured()
        {
            // Arrange
            var dbSettings = new DatabaseSettings
            {
                Provider = "SQLite",
                ConnectionString = "Data Source=:memory:"
            };
            var options = Options.Create(dbSettings);

            var factory = new ConnectionFactory(options);

            // Act
            using var connection = factory.CreateConnection();

            // Assert
            Assert.NotNull(connection);
            Assert.Contains("SqliteConnection", connection.GetType().Name);
        }
        #endregion

        #region EfRepository Tests
        [Fact]
        public async Task EfRepository_ShouldPerformBasicCrudCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NexusDbContext>()
                .UseInMemoryDatabase(databaseName: "nexus_test_db_" + Guid.NewGuid())
                .Options;

            using var context = new NexusDbContext(options);
            var repository = new EfRepository<AccountDbModel>(context);

            var accountId = Guid.NewGuid();
            var accountDb = new AccountDbModel
            {
                Id = accountId,
                BrokerAccountId = "ACC_GENERIC_TEST",
                BrokerName = "TestBroker",
                Currency = "EUR",
                Balance = 10000m,
                Equity = 10000m,
                Margin = 0m,
                FreeMargin = 10000m,
                Leverage = 100,
                IsLive = false,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // Act: Add
            await repository.AddAsync(accountDb);
            await context.SaveChangesAsync();

            // Act: GetById
            var retrieved = await repository.GetByIdAsync(accountId.ToString());
            Assert.NotNull(retrieved);
            Assert.Equal("ACC_GENERIC_TEST", retrieved.BrokerAccountId);

            // Act: GetAll
            var all = await repository.GetAllAsync();
            Assert.Single(all);

            // Act: Update
            retrieved.Balance = 15000m;
            await repository.UpdateAsync(retrieved);
            await context.SaveChangesAsync();

            var updated = await repository.GetByIdAsync(accountId.ToString());
            Assert.NotNull(updated);
            Assert.Equal(15000m, updated.Balance);

            // Act: Delete
            await repository.DeleteAsync(updated);
            await context.SaveChangesAsync();

            var deleted = await repository.GetByIdAsync(accountId.ToString());
            Assert.Null(deleted);
        }
        #endregion

        #region Configuration Settings Validation
        [Fact]
        public void ConfigurationSettings_ShouldInstantiateWithDefaults()
        {
            // Arrange & Act
            var dbSettings = new DatabaseSettings();
            var logSettings = new LoggingSettings();
            var appSettings = new ApplicationSettings();

            // Assert
            Assert.Equal("PostgreSQL", dbSettings.Provider);
            Assert.Equal("Information", logSettings.DefaultLevel);
            Assert.Equal("Development", appSettings.Environment);
            Assert.False(appSettings.IsLiveTradingEnabled);
        }
        #endregion
    }
}
