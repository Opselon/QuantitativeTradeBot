namespace Nexus.Application.Ports
{
    public interface IDatabaseBootstrapper
    {
        string ProviderName { get; }
        Task InitializeDatabaseAsync(string connectionString, CancellationToken cancellationToken = default);
        Task MigrateDatabaseAsync(string connectionString, CancellationToken cancellationToken = default);
        Task<bool> IsMigrationRequiredAsync(string connectionString, CancellationToken cancellationToken = default);
    }
}
