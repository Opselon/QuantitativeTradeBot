using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Application.Ports
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default);
        Task AddAsync(Order order, CancellationToken cancellationToken = default);
        Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Order>> GetOpenOrdersAsync(Symbol? symbol = null, CancellationToken cancellationToken = default);
    }
}
