using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nexus.Infrastructure.Persistence
{
    public class DesignTimeNexusDbContextFactory : IDesignTimeDbContextFactory<NexusDbContext>
    {
        public NexusDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<NexusDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres");

            return new NexusDbContext(optionsBuilder.Options);
        }
    }
}
