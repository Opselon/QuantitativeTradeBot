using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexus.Application.Strategies
{
    public interface IStrategyStateStore
    {
        Task SaveStateAsync(string strategyId, Dictionary<string, string> state);
        Task<Dictionary<string, string>?> LoadStateAsync(string strategyId);
    }
}
