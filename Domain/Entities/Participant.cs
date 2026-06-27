using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Participant : BaseEntity
    {
        [Key]
        public Guid ParticipantID { get; set; }

        [Required]
        public Guid AppointmentID { get; set; }

        public Guid? UserID { get; set; }

        public string? Email { get; set; }

        public string? FullName { get; set; }

        public string? InvitationStatus { get; set; } = "pending";

        public DateTimeOffset? ResponseReceivedAt { get; set; }

        public string? ParticipantRole { get; set; } = "attendee";
        // REMOVED: CreatedAt, UpdatedAt, IsDeleted, DeletedAt (inherited from BaseEntity)

        [ForeignKey("AppointmentID")]
        public virtual Appointment? Appointment { get; set; }

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}