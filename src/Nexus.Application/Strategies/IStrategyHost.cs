using Nexus.Core.Entities;

namespace Nexus.Application.Strategies
{
    public interface IStrategyHost
    {
        string StrategyId { get; }
        StrategyDescriptor Descriptor { get; }
        bool IsRunning { get; }
        bool IsPaused { get; }

        Task InitializeAsync();
        Task ProcessTickAsync(Tick tick, string correlationId);
        Task ProcessBarAsync(Bar bar, string correlationId);
        Task StartAsync();
        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();
    }
}
