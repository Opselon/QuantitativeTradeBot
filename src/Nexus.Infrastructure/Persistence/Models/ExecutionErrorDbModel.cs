using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexus.Infrastructure.Persistence.Models
{
    [Table("execution_errors")]
    public class ExecutionErrorDbModel
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("order_id")]
        [MaxLength(64)]
        public string? OrderId { get; set; }

        [Column("error_code")]
        [MaxLength(64)]
        [Required]
        public string ErrorCode { get; set; } = string.Empty;

        [Column("error_message")]
        [Required]
        public string ErrorMessage { get; set; } = string.Empty;

        [Column("timestamp_utc")]
        public DateTime TimestampUtc { get; set; }
    }
}
