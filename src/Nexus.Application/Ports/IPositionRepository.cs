using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Application.Ports
{
    public interface IPositionRepository
    {
        Task<Position?> GetByIdAsync(string positionId, CancellationToken cancellationToken = default);
        Task AddAsync(Position position, CancellationToken cancellationToken = default);
        Task UpdateAsync(Position position, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Position>> GetOpenPositionsAsync(Symbol? symbol = null, CancellationToken cancellationToken = default);
    }
}
