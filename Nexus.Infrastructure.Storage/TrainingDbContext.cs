using Microsoft.EntityFrameworkCore;
using Nexus.Infrastructure.Storage.Models;

namespace Nexus.Infrastructure.Storage
{
    /// <summary>
    /// Dedicated database context for AI artifacts and training metadata.
    /// Strictly separated from the operational market database.
    /// </summary>
    public class TrainingDbContext : DbContext
    {
        public DbSet<DatasetMetadataDbModel> Datasets { get; set; } = null!;
        public DbSet<ModelMetadataDbModel> Models { get; set; } = null!;
        public DbSet<ExperimentDbModel> Experiments { get; set; } = null!;

        public TrainingDbContext(DbContextOptions<TrainingDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DatasetMetadataDbModel>().HasKey(e => e.DatasetId);
            modelBuilder.Entity<ModelMetadataDbModel>().HasKey(e => e.ModelId);
            modelBuilder.Entity<ExperimentDbModel>().HasKey(e => e.ExperimentId);

            // Indexing for rapid Champion retrieval
            modelBuilder.Entity<ModelMetadataDbModel>()
                .HasIndex(e => e.Status);
        }
    }
}