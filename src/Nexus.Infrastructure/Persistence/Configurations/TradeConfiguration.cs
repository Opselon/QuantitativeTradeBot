using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class TradeConfiguration : IEntityTypeConfiguration<TradeDbModel>
    {
        public void Configure(EntityTypeBuilder<TradeDbModel> builder)
        {
            builder.ToTable("trades");

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

            builder.Property(e => e.Volume)
                .HasColumnName("volume")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(e => e.Price)
                .HasColumnName("price")
                .IsRequired();

            builder.Property(e => e.Commission)
                .HasColumnName("commission")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(e => e.Swap)
                .HasColumnName("swap")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(e => e.RealizedPnl)
                .HasColumnName("realized_pnl")
                .HasColumnType("numeric(20,4)")
                .IsRequired();

            builder.Property(e => e.ExecutedAtUtc)
                .HasColumnName("executed_at_utc")
                .IsRequired();

            builder.Property(e => e.PositionId)
                .HasColumnName("position_id");

            builder.HasOne<PositionDbModel>()
                .WithMany()
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
