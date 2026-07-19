using Microsoft.EntityFrameworkCore;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly NexusDbContext _context;

        public OrderRepository(NexusDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default)
        {
            OrderDbModel? dbModel = null;
            if (Guid.TryParse(orderId, out var guidId))
            {
                dbModel = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == guidId, cancellationToken);
            }

            if (dbModel == null)
            {
                dbModel = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.TicketId == orderId, cancellationToken);
            }

            return dbModel?.ToDomain();
        }

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            var dbModel = OrderDbModel.FromDomain(order);
            await _context.Orders.AddAsync(dbModel, cancellationToken);
        }

        public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            var existing = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

            if (existing != null)
            {
                existing.TicketId = order.TicketId;
                existing.Status = order.Status.ToString();
                existing.StatusReason = order.StatusReason;
                existing.Price = order.Price;
                existing.StopLoss = order.StopLoss;
                existing.TakeProfit = order.TakeProfit;
                existing.UpdatedAtUtc = order.UpdatedAt.Kind == DateTimeKind.Utc
                    ? order.UpdatedAt
                    : DateTime.SpecifyKind(order.UpdatedAt, DateTimeKind.Utc);
            }
        }

        public async Task<IReadOnlyList<Order>> GetOpenOrdersAsync(Symbol? symbol = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Pending");

            if (symbol != null)
            {
                var symbolName = symbol.Name;
                query = query.Where(o => o.Symbol == symbolName);
            }

            var list = await query.ToListAsync(cancellationToken);
            return list.Select(o => o.ToDomain()).ToList();
        }
    }
}
