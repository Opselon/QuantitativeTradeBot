using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class ExecutionErrorConfiguration : IEntityTypeConfiguration<ExecutionErrorDbModel>
    {
        public void Configure(EntityTypeBuilder<ExecutionErrorDbModel> builder)
        {
            builder.ToTable("execution_errors");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.OrderId)
                .HasColumnName("order_id")
                .HasMaxLength(64);

            builder.Property(e => e.ErrorCode)
                .HasColumnName("error_code")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .IsRequired();

            builder.Property(e => e.TimestampUtc)
                .HasColumnName("timestamp_utc")
                .IsRequired();
        }
    }
}
