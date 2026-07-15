using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexus.Infrastructure.Persistence.Models
{
    [Table("ExperienceRecords")]
    public class ExperienceDbModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(16)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        public DateTime TimestampUtc { get; set; }

        // Vector stored as flat comma-separated values (CSV) for lightweight parsing
        [Required]
        public string MarketVectorCsv { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string ModelVersion { get; set; } = "1.0.0";

        public double BuyConfidence { get; set; }
        public double SellConfidence { get; set; }
        public double RiskScore { get; set; }

        [Required]
        [MaxLength(32)]
        public string MarketRegime { get; set; } = "Unknown";

        [Required]
        [MaxLength(16)]
        public string ExecutedAction { get; set; } = "WAIT";

        public double RealizedPips { get; set; }
        public bool IsCompleted { get; set; }
    }
}