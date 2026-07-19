using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class PositionConfiguration : IEntityTypeConfiguration<PositionDbModel>
    {
        public void Configure(EntityTypeBuilder<PositionDbModel> builder)
        {
            builder.ToTable("positions");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.TicketId)
                .HasColumnName("ticket_id")
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(e => e.TicketId)
                .IsUnique();

            builder.Property(e => e.Symbol)
                .HasColumnName("symbol")
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(e => e.Direction)
                .HasColumnName("direction")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(e => e.Volume)
                .HasColumnName("volume")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(e => e.EntryPrice)
                .HasColumnName("entry_price")
                .IsRequired();

            builder.Property(e => e.CurrentPrice)
                .HasColumnName("current_price")
                .IsRequired();

            builder.Property(e => e.StopLoss)
                .HasColumnName("stop_loss");

            builder.Property(e => e.TakeProfit)
                .HasColumnName("take_profit");

            builder.Property(e => e.UnrealizedPnl)
                .HasColumnName("unrealized_pnl")
                .HasColumnType("numeric(20,4)")
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("OPEN")
                .IsRequired();

            builder.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            builder.Property(e => e.OpenedAtUtc)
                .HasColumnName("opened_at_utc")
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

            var isPostgres = builder.Metadata.Model.GetAnnotations().Any(a =>
                a.Name.Contains("Npgsql") ||
                a.Value?.ToString()?.Contains("Npgsql") == true);

            if (isPostgres)
            {
                // Optimistic Concurrency Token via xmin system column
                builder.Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .IsRowVersion();
            }
            else
            {
                builder.Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasDefaultValue(0u);
            }
        }
    }
}
