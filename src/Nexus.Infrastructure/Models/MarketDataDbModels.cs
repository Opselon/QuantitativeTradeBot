using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexus.Infrastructure.Persistence.Models
{
    #region Tick Database Model & Configuration
    /// <summary>
    /// Represents a persisted high-frequency price tick.
    /// </summary>
    public class TickDbModel
    {
        public long Id { get; set; } // Auto-incrementing primary key
        public string Symbol { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Volume { get; set; }
    }

    public class TickConfiguration : IEntityTypeConfiguration<TickDbModel>
    {
        public void Configure(EntityTypeBuilder<TickDbModel> builder)
        {
            builder.ToTable("ticks");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Symbol).HasMaxLength(16).IsRequired();
            builder.Property(e => e.TimestampUtc).IsRequired();

            // HFT RANGE QUERY INDEX: Highly optimized for range queries on Symbol + Timestamp
            builder.HasIndex(e => new { e.Symbol, e.TimestampUtc });
        }
    }
    #endregion

    #region Candle Database Model & Configuration
    /// <summary>
    /// Represents persisted historical OHLCV candle bar data.
    /// </summary>
    public class CandleDbModel
    {
        public long Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Timeframe { get; set; } = "M1";
        public DateTime TimestampUtc { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
    }

    public class CandleConfiguration : IEntityTypeConfiguration<CandleDbModel>
    {
        public void Configure(EntityTypeBuilder<CandleDbModel> builder)
        {
            builder.ToTable("candles");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Symbol).HasMaxLength(16).IsRequired();
            builder.Property(e => e.Timeframe).HasMaxLength(8).IsRequired();
            builder.Property(e => e.TimestampUtc).IsRequired();

            // COMPOSITE RENDERING INDEX: Highly optimized composite index for rendering historical candles
            builder.HasIndex(e => new { e.Symbol, e.Timeframe, e.TimestampUtc }).IsUnique();
        }
    }
    #endregion
}