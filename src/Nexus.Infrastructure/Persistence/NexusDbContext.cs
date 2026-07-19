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
        public DbSet<ExperienceDbModel> ExperienceRecords => Set<ExperienceDbModel>(); // Added Experience Dataset Set
        public DbSet<ExecutionErrorDbModel> ExecutionErrors => Set<ExecutionErrorDbModel>();
        public DbSet<Models.TickDbModel> Ticks { get; set; } = null!;
        public DbSet<Models.CandleDbModel> Candles { get; set; } = null!;

        public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply existing configs...
            modelBuilder.ApplyConfiguration(new Configurations.OrderConfiguration());

            // REASON: Apply new high-performance market data configurations and indexes
            modelBuilder.ApplyConfiguration(new Models.TickConfiguration());
            modelBuilder.ApplyConfiguration(new Models.CandleConfiguration());
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