using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Participant : BaseEntity
    {
        [Key]
        public Guid ParticipantId { get; set; }

        [Required]
        public Guid AppointmentId { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [Required]
        [MaxLength(20)]
        public string InvitationStatus { get; set; } = "pending";

        public DateTimeOffset? ResponseReceivedAt { get; set; }

        [Required]
        [MaxLength(20)]
        public string ParticipantRole { get; set; } = "attendee";

        // Navigation properties
        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}