using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    public class PositionRepository : IPositionRepository
    {
        private readonly NexusDbContext _context;

        public PositionRepository(NexusDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Position?> GetByIdAsync(string positionId, CancellationToken cancellationToken = default)
        {
            PositionDbModel? dbModel = null;
            if (Guid.TryParse(positionId, out var guidId))
            {
                dbModel = await _context.Positions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == guidId, cancellationToken);
            }

            if (dbModel == null)
            {
                dbModel = await _context.Positions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.TicketId == positionId, cancellationToken);
            }

            return dbModel?.ToDomain();
        }

        public async Task AddAsync(Position position, CancellationToken cancellationToken = default)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));
            var dbModel = PositionDbModel.FromDomain(position);
            await _context.Positions.AddAsync(dbModel, cancellationToken);
        }

        public async Task UpdateAsync(Position position, CancellationToken cancellationToken = default)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));

            var existing = await _context.Positions
                .FirstOrDefaultAsync(p => p.Id == position.Id, cancellationToken);

            if (existing != null)
            {
                existing.CurrentPrice = position.CurrentPrice;
                existing.StopLoss = position.StopLoss;
                existing.TakeProfit = position.TakeProfit;
                existing.UnrealizedPnl = position.UnrealizedPnl;
                existing.UpdatedAtUtc = position.UpdatedAt.Kind == DateTimeKind.Utc
                    ? position.UpdatedAt
                    : DateTime.SpecifyKind(position.UpdatedAt, DateTimeKind.Utc);
            }
        }

        public async Task<IReadOnlyList<Position>> GetOpenPositionsAsync(Symbol? symbol = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Positions
                .AsNoTracking()
                .Where(p => p.Status == "OPEN");

            if (symbol != null)
            {
                var symbolName = symbol.Name;
                query = query.Where(p => p.Symbol == symbolName);
            }

            var list = await query.ToListAsync(cancellationToken);
            return list.Select(p => p.ToDomain()).ToList();
        }
    }
}
