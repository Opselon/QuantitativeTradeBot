using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    public interface ITrailingManager
    {
        Task ProcessTrailingStopAsync(Position position, Tick currentTick);
    }
}
