using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Nexus.Application.Ports;
using Nexus.Application.Workflows;
using Nexus.Application.Workflows.DTOs;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Security;
using Nexus.Infrastructure.Adapters.Mt5;

namespace Nexus.Tests.Unit.Desktop
{
    public class DesktopTests
    {
        [Fact]
        public async Task SecretStore_CanSaveAndRetrieveSecret_UsingCrossPlatformFallback()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"secrets_{Guid.NewGuid():N}.dat");
            var store = new WindowsSecretStore(tempFile);
            var key = "TestKey";
            var secretValue = "SuperSecurePassword123!";

            try
            {
                // Act
                await store.SaveSecretAsync(key, secretValue);
                var retrieved = await store.GetSecretAsync(key);

                // Assert
                Assert.Equal(secretValue, retrieved);

                // Clean/Delete
                await store.DeleteSecretAsync(key);
                var afterDelete = await store.GetSecretAsync(key);
                Assert.Null(afterDelete);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SqliteBootstrapper_CanInitializeDatabase_WithLocalFile()
        {
            // Arrange
            var tempDbFile = Path.Combine(Path.GetTempPath(), $"nexus_test_{Guid.NewGuid():N}.db");
            var connectionString = $"Data Source={tempDbFile}";
            var bootstrapper = new SqliteDatabaseBootstrapper();

            try
            {
                // Act & Assert (1. Verify migration is required)
                var isReqBefore = await bootstrapper.IsMigrationRequiredAsync(connectionString);
                Assert.True(isReqBefore);

                // 2. Initialize
                await bootstrapper.InitializeDatabaseAsync(connectionString);

                // 3. Verify migration is NO longer required
                var isReqAfter = await bootstrapper.IsMigrationRequiredAsync(connectionString);
                Assert.False(isReqAfter);
            }
            finally
            {
                // Ensure db file is closed/disposed and cleaned up
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(tempDbFile))
                {
                    try
                    {
                        File.Delete(tempDbFile);
                    }
                    catch { /* retry fallback or ignore */ }
                }
            }
        }

        [Fact]
        public async Task SimulatedMt5ConnectionService_ReturnsCorrectOutcomes_BasedOnProfileInput()
        {
            // Arrange
            var service = new SimulatedMt5ConnectionService();

            var invalidProfile = new ConnectionProfileDto
            {
                ProfileName = "Invalid",
                BrokerServer = "", // empty
                LoginAccountId = "12345",
                Password = "pwd"
            };

            var validProfile = new ConnectionProfileDto
            {
                ProfileName = "Valid",
                BrokerServer = "ICMarkets-Demo",
                LoginAccountId = "7820491",
                Password = "valid_password"
            };

            // Act
            var resultInvalid = await service.TestConnectionAsync(invalidProfile);
            var resultValid = await service.TestConnectionAsync(validProfile);

            // Assert
            Assert.False(resultInvalid.IsSuccess);
            Assert.Contains("server cannot be empty", resultInvalid.ErrorMessage);

            Assert.True(resultValid.IsSuccess);
            Assert.NotNull(resultValid.AccountSnapshot);
            Assert.Equal("7820491", resultValid.AccountSnapshot.AccountId);
            Assert.Equal(10000.00m, resultValid.AccountSnapshot.Balance);
            Assert.Equal("Demo", resultValid.AccountSnapshot.AccountMode);
        }

        [Fact]
        public async Task GetPersistenceOptionsQuery_ReturnsPostgresAsRecommended()
        {
            // Arrange
            var configService = new AppConfigurationService(Path.Combine(Path.GetTempPath(), $"config_{Guid.NewGuid():N}.json"));
            var query = new GetPersistenceOptionsQuery(configService);

            // Act
            var options = await query.ExecuteAsync();

            // Assert
            Assert.Equal(2, options.Count);

            var postgres = options.Find(o => o.ProviderName == "PostgreSQL");
            Assert.NotNull(postgres);
            Assert.True(postgres.IsRecommended);

            var sqlite = options.Find(o => o.ProviderName == "SQLite");
            Assert.NotNull(sqlite);
            Assert.False(sqlite.IsRecommended);
        }
    }
}
