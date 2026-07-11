using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly NexusDbContext _context;

        public AccountRepository(NexusDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default)
        {
            AccountDbModel? dbModel = null;
            if (Guid.TryParse(accountId, out var guidId))
            {
                dbModel = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == guidId, cancellationToken);
            }

            if (dbModel == null)
            {
                dbModel = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.BrokerAccountId == accountId, cancellationToken);
            }

            return dbModel?.ToDomain();
        }

        public async Task UpsertAsync(Account account, CancellationToken cancellationToken = default)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));

            var existing = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

            if (existing == null)
            {
                var dbModel = AccountDbModel.FromDomain(account);
                await _context.Accounts.AddAsync(dbModel, cancellationToken);
            }
            else
            {
                existing.Balance = account.Balance;
                existing.Equity = account.Equity;
                existing.Margin = account.Margin;
                existing.FreeMargin = account.FreeMargin;
                existing.UpdatedAtUtc = account.UpdatedAt.Kind == DateTimeKind.Utc
                    ? account.UpdatedAt
                    : DateTime.SpecifyKind(account.UpdatedAt, DateTimeKind.Utc);
                // Also update other fields in case they changed
                existing.IsLive = account.IsLive;
                existing.Leverage = account.Leverage;
                existing.BrokerAccountId = account.BrokerAccountId;
                existing.BrokerName = account.BrokerName;
                existing.Currency = account.Currency;
            }
        }
    }
}
