using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.AI.Interfaces;
using Nexus.Infrastructure.Storage.Repositories;

namespace Nexus.Infrastructure.Storage
{
    public static class StorageDependencyInjection
    {
        public static IServiceCollection AddNexusAiStorage(this IServiceCollection services, string connectionString)
        {
            // Fully isolated context for AI training data
            services.AddDbContext<TrainingDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddScoped<IModelRegistry, ModelRegistry>();

            // To be implemented fully in subsequent phase
            // services.AddScoped<IDatasetRegistry, DatasetRegistry>();
            // services.AddScoped<IExperimentTracker, ExperimentTracker>();

            return services;
        }
    }
}