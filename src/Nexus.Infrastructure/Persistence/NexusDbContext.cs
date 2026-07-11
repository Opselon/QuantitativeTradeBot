using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence
{
    public class NexusDbContext : DbContext
    {
        public DbSet<AccountDbModel> Accounts => Set<AccountDbModel>();
        public DbSet<OrderDbModel> Orders => Set<OrderDbModel>();
        public DbSet<PositionDbModel> Positions => Set<PositionDbModel>();
        public DbSet<TradeDbModel> Trades => Set<TradeDbModel>();

        public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusDbContext).Assembly);
        }

        public override int SaveChanges()
        {
            EnsureUtcTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnsureUtcTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void EnsureUtcTimestamps()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        if (property.CurrentValue is DateTime dt)
                        {
                            if (dt.Kind == DateTimeKind.Local)
                            {
                                throw new InvalidOperationException($"Cannot save Local DateTime for property '{property.Metadata.Name}' on entity '{entry.Entity.GetType().Name}'. All timestamps must be UTC.");
                            }
                            if (dt.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                            }
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        if (property.CurrentValue is DateTime dtNullable)
                        {
                            if (dtNullable.Kind == DateTimeKind.Local)
                            {
                                throw new InvalidOperationException($"Cannot save Local DateTime for property '{property.Metadata.Name}' on entity '{entry.Entity.GetType().Name}'. All timestamps must be UTC.");
                            }
                            if (dtNullable.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dtNullable, DateTimeKind.Utc);
                            }
                        }
                    }
                }
            }
        }
    }
}
