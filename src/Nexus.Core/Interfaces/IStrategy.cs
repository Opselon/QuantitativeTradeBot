using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    public interface IStrategy
    {
        string Name { get; }
        bool IsEnabled { get; }

        Task OnInitializeAsync();
        Task OnTickAsync(Tick tick);
        Task OnBarAsync(Bar bar);
        Task OnStopAsync();
    }
}
