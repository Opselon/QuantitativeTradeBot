using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Ports;
using Nexus.Infrastructure.Configuration;
using Nexus.Infrastructure.Logging;
using Nexus.Infrastructure.Persistence.Repositories;
using Nexus.Infrastructure.Storage.FileStorage;

namespace Nexus.Infrastructure.Persistence
{
    /// <summary>
    /// Dependency Injection extension methods for registering persistence, logging, configuration, and storage services.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all foundational infrastructure services, settings options, and database configuration mappings.
        /// </summary>
        public static IServiceCollection AddNexusPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Options Pattern Bindings
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
            services.Configure<LoggingSettings>(configuration.GetSection("Logging"));
            services.Configure<ApplicationSettings>(configuration.GetSection("Application"));

            // Database Context Setup
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";

            services.AddDbContext<NexusDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Infrastructure Foundations Registrations
            services.AddSingleton<IApplicationLogger, ApplicationLogger>();
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            services.AddScoped<IDatabaseProvider, DatabaseProvider>();
            services.AddScoped<IConnectionFactory, ConnectionFactory>();

            // Repository and UnitOfWork Registrations
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPositionRepository, PositionRepository>();
            services.AddScoped<IMarketDataRepository, MarketDataRepository>();
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<Nexus.Execution.Auditing.IExecutionAuditService, DbExecutionAuditService>();

            return services;
        }
    }
}
