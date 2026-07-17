using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.AI.Interfaces;
using Nexus.Infrastructure.TorchSharp.Inference;
using Nexus.Infrastructure.TorchSharp.Training;

namespace Nexus.Infrastructure.TorchSharp
{
    public static class TorchDependencyInjection
    {
        public static IServiceCollection AddNexusTorchBackend(this IServiceCollection services)
        {
            // Registered as Singleton since it loads models into memory and guards them with locks
            services.AddSingleton<IInferenceEngine, TorchInferenceEngine>();

            // Registered as Transient/Scoped so each training run gets a clean pipeline instance
            services.AddTransient<ITrainingPipeline, TorchTrainingPipeline>();

            return services;
        }
    }
}