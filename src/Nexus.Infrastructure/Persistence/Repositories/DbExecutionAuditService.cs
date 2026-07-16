using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Execution.Auditing;
using Nexus.Execution.Domain;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    public class DbExecutionAuditService : IExecutionAuditService
    {
        private readonly NexusDbContext _context;

        public DbExecutionAuditService(NexusDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task RecordOrderAsync(OrderRequest request, Guid? accountId = null, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var existing = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
            if (existing != null)
            {
                existing.Status = request.State.ToString();
                existing.StatusReason = request.Reason;
                existing.StopLoss = request.StopLoss;
                existing.TakeProfit = request.TakeProfit;
                existing.UpdatedAtUtc = DateTime.SpecifyKind(request.UpdatedAt, DateTimeKind.Utc);
            }
            else
            {
                var dbModel = new OrderDbModel
                {
                    Id = request.Id,
                    TicketId = request.Id.ToString("N"),
                    Symbol = request.Symbol,
                    Direction = request.Side,
                    Type = "Market",
                    Volume = (decimal)request.Volume,
                    Price = request.Entry,
                    StopLoss = request.StopLoss,
                    TakeProfit = request.TakeProfit,
                    Status = request.State.ToString(),
                    StatusReason = request.Reason,
                    CreatedAtUtc = DateTime.SpecifyKind(request.CreatedAt, DateTimeKind.Utc),
                    UpdatedAtUtc = DateTime.SpecifyKind(request.UpdatedAt, DateTimeKind.Utc),
                    AccountId = accountId
                };
                await _context.Orders.AddAsync(dbModel, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordExecutionErrorAsync(string? orderId, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            var dbModel = new ExecutionErrorDbModel
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ErrorCode = errorCode ?? "ERROR",
                ErrorMessage = errorMessage ?? "Unknown execution error.",
                TimestampUtc = DateTime.UtcNow
            };

            await _context.ExecutionErrors.AddAsync(dbModel, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordPositionAsync(PositionSnapshot snapshot, Guid? accountId = null, CancellationToken cancellationToken = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var existing = await _context.Positions.FirstOrDefaultAsync(p => p.TicketId == snapshot.TicketId, cancellationToken);
            if (existing != null)
            {
                existing.CurrentPrice = snapshot.CurrentPrice;
                existing.StopLoss = snapshot.StopLoss;
                existing.TakeProfit = snapshot.TakeProfit;
                existing.UnrealizedPnl = snapshot.UnrealizedPnl;
                existing.Status = snapshot.Status;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                var dbModel = new PositionDbModel
                {
                    Id = Guid.NewGuid(),
                    TicketId = snapshot.TicketId,
                    Symbol = snapshot.Symbol,
                    Direction = snapshot.Direction,
                    Volume = (decimal)snapshot.Volume,
                    EntryPrice = snapshot.EntryPrice,
                    CurrentPrice = snapshot.CurrentPrice,
                    StopLoss = snapshot.StopLoss,
                    TakeProfit = snapshot.TakeProfit,
                    UnrealizedPnl = snapshot.UnrealizedPnl,
                    Status = snapshot.Status,
                    CreatedAtUtc = DateTime.UtcNow,
                    OpenedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    AccountId = accountId
                };
                await _context.Positions.AddAsync(dbModel, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
