using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Reminder : BaseEntity
    {
        [Key]
        public Guid ReminderId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? TaskId { get; set; }

        public Guid? AppointmentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReminderType { get; set; } = "general"; // task, appointment, general

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }

        [Required]
        public DateTimeOffset ReminderDateTime { get; set; }

        public bool NotifyViaEmail { get; set; } = true;

        public bool NotifyViaPush { get; set; } = true;

        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, triggered, sent, failed, cancelled

        public bool IsRecurring { get; set; } = false;

        public DateTimeOffset? SentAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskEntity? Task { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
    }
}