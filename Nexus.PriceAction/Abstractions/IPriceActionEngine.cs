using System.Threading;
using System.Threading.Tasks;

namespace Nexus.PriceAction.Abstractions
{
    public interface IPriceActionEngine
    {
        Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default);
    }
}