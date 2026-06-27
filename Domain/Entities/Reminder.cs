using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Reminder : BaseEntity
    {
        [Key]
        public Guid ReminderID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        public Guid? TaskID { get; set; }

        public Guid? AppointmentID { get; set; }

        public string? ReminderType { get; set; } = "general";

        public string? Title { get; set; }


        // NEW: Links to Meeting and Event
        public Guid? MeetingID { get; set; }
        public Guid? EventID { get; set; }

        public string? Message { get; set; }

        [Required]
        public DateTimeOffset ReminderDateTime { get; set; }

        public bool NotifyViaEmail { get; set; } = true;

        public bool NotifyViaPush { get; set; } = true;

        public string? Status { get; set; } = "pending";

        public bool IsRecurring { get; set; } = false;

        public DateTimeOffset? SentAt { get; set; }
        // REMOVED: CreatedAt, UpdatedAt, IsDeleted, DeletedAt (inherited from BaseEntity)

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        [ForeignKey("TaskID")]
        public virtual TaskEntity? Task { get; set; }

        [ForeignKey("AppointmentID")]
        public virtual Appointment? Appointment { get; set; }

        [ForeignKey("MeetingID")]
        public virtual Meeting? Meeting { get; set; }

        [ForeignKey("EventID")]
        public virtual Event? Event { get; set; }

        // NEW navigation properties
       
    }
}