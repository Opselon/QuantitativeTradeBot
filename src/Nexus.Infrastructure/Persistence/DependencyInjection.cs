using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Ports;
using Nexus.Infrastructure.Persistence.Repositories;

namespace Nexus.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNexusPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";

            services.AddDbContext<NexusDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPositionRepository, PositionRepository>();
            services.AddScoped<IMarketDataRepository, MarketDataRepository>();

            return services;
        }
    }
}
