using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    public record RiskResult(bool IsPassed, string Reason);

    public interface IRiskManager
    {
        Task<RiskResult> CheckOrderRiskAsync(Account account, Order order);
    }
}
