using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<OrderDbModel>
    {
        public void Configure(EntityTypeBuilder<OrderDbModel> builder)
        {
            builder.ToTable("orders");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.TicketId)
                .HasColumnName("ticket_id")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.Symbol)
                .HasColumnName("symbol")
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Direction)
                .HasColumnName("direction")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Volume)
                .HasColumnName("volume")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(e => e.Price)
                .HasColumnName("price")
                .IsRequired();

            builder.Property(e => e.StopLoss)
                .HasColumnName("stop_loss");

            builder.Property(e => e.TakeProfit)
                .HasColumnName("take_profit");

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.StatusReason)
                .HasColumnName("status_reason")
                .IsRequired();

            builder.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            builder.Property(e => e.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .IsRequired();

            builder.Property(e => e.AccountId)
                .HasColumnName("account_id");

            builder.HasOne<AccountDbModel>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // Optimistic Concurrency Token via xmin system column
            builder.Property<uint>("xmin")
                .HasColumnName("xmin")
                .IsRowVersion();
        }
    }
}
