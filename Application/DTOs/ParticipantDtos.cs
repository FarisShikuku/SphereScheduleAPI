using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class ParticipantDto
    {
        public Guid ParticipantId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string InvitationStatus { get; set; } = "pending";
        public DateTimeOffset? ResponseReceivedAt { get; set; }
        public string ParticipantRole { get; set; } = "attendee";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Additional info
        public string? UserDisplayName { get; set; }
        public string? UserAvatarUrl { get; set; }
        public string AppointmentTitle { get; set; } = string.Empty;
        public DateTimeOffset AppointmentStartDateTime { get; set; }
        public DateTimeOffset AppointmentEndDateTime { get; set; }
        public string? AppointmentLocation { get; set; }
        public bool AppointmentIsVirtual { get; set; }
        public string? AppointmentMeetingLink { get; set; }
    }

    public class CreateParticipantDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [RegularExpression("^(pending|sent|accepted|declined|tentative)$", ErrorMessage = "Invalid invitation status")]
        public string? InvitationStatus { get; set; } = "pending";

        [RegularExpression("^(organizer|attendee|optional)$", ErrorMessage = "Invalid participant role")]
        public string? ParticipantRole { get; set; } = "attendee";

        public Guid? UserId { get; set; }
    }

    public class UpdateParticipantDto
    {
        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? FullName { get; set; }

        [RegularExpression("^(pending|sent|accepted|declined|tentative)$", ErrorMessage = "Invalid invitation status")]
        public string? InvitationStatus { get; set; }

        [RegularExpression("^(organizer|attendee|optional)$", ErrorMessage = "Invalid participant role")]
        public string? ParticipantRole { get; set; }

        public Guid? UserId { get; set; }
    }

    public class UpdateParticipantStatusDto
    {
        [Required]
        [RegularExpression("^(accepted|declined|tentative)$", ErrorMessage = "Status must be 'accepted', 'declined', or 'tentative'")]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ResponseMessage { get; set; }
    }

    public class BulkAddParticipantsDto
    {
        [Required]
        public Guid AppointmentId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one participant is required")]
        public List<CreateParticipantDto> Participants { get; set; } = new();

        public bool SendInvitations { get; set; } = true;
    }

    public class ParticipantFilterDto
    {
        public Guid? AppointmentId { get; set; }
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? InvitationStatus { get; set; }
        public string? ParticipantRole { get; set; }
        public bool? IncludeAppointmentDetails { get; set; } = false;
        public bool? IncludeUserDetails { get; set; } = false;
        public DateTimeOffset? ResponseAfter { get; set; }
        public DateTimeOffset? ResponseBefore { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = false;
    }

    public class ParticipantStatisticsDto
    {
        public int TotalParticipants { get; set; }
        public int AcceptedCount { get; set; }
        public int DeclinedCount { get; set; }
        public int PendingCount { get; set; }
        public int TentativeCount { get; set; }
        public int OrganizerCount { get; set; }
        public int AttendeeCount { get; set; }
        public int OptionalCount { get; set; }
        public double AcceptanceRate { get; set; }
        public Dictionary<string, int> ParticipantsByStatus { get; set; } = new();
        public Dictionary<string, int> ParticipantsByRole { get; set; } = new();
    }
}