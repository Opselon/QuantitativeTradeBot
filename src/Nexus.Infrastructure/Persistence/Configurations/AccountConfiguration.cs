using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Infrastructure.Persistence.Models;

namespace Nexus.Infrastructure.Persistence.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<AccountDbModel>
    {
        public void Configure(EntityTypeBuilder<AccountDbModel> builder)
        {
            builder.ToTable("accounts");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.BrokerAccountId)
                .HasColumnName("broker_account_id")
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(e => e.BrokerAccountId)
                .IsUnique();

            builder.Property(e => e.BrokerName)
                .HasColumnName("broker_name")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(e => e.Balance)
                .HasColumnName("balance")
                .HasColumnType("numeric(20,4)")
                .IsRequired();

            builder.Property(e => e.Equity)
                .HasColumnName("equity")
                .HasColumnType("numeric(20,4)")
                .IsRequired();

            builder.Property(e => e.Margin)
                .HasColumnName("margin")
                .HasColumnType("numeric(20,4)")
                .IsRequired();

            builder.Property(e => e.FreeMargin)
                .HasColumnName("free_margin")
                .HasColumnType("numeric(20,4)")
                .IsRequired();

            builder.Property(e => e.Leverage)
                .HasColumnName("leverage")
                .IsRequired();

            builder.Property(e => e.IsLive)
                .HasColumnName("is_live")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(e => e.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .IsRequired();

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
