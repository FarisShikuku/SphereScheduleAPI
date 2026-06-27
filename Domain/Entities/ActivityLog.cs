using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SphereScheduleAPI.Domain.Entities
{
    public class ActivityLog  // Not inheriting BaseEntity (no duplicates)
    {
        [Key]
        public long LogId { get; set; }

        public Guid? UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EntityType { get; set; }

        public Guid? EntityId { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public string? Details { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "success";

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}