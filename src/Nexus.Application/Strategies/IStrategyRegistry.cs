using Nexus.Core.Interfaces;

namespace Nexus.Application.Strategies
{
    public interface IStrategyRegistry
    {
        void Register(StrategyDescriptor descriptor, IStrategy strategy);
        IReadOnlyList<StrategyDescriptor> GetDescriptors();
        IStrategy? GetStrategy(string strategyId);
        StrategyDescriptor? GetDescriptor(string strategyId);
    }
}
