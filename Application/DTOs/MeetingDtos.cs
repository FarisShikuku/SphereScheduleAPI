// Application/DTOs/MeetingDtos.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    // ─── Response DTO ───────────────────────────────────────────────────────
    public class MeetingDto
    {
        public Guid MeetingID { get; set; }
        public Guid TaskID { get; set; }
        public Guid OrganizerUserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public string? MeetingLink { get; set; }
        public string? MeetingPlatform { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Related data
        public string? OrganizerName { get; set; }
        public string? TaskTitle { get; set; }
        public int ParticipantCount { get; set; }
        public List<MeetingParticipantDto> Participants { get; set; } = new();
    }

    public class MeetingParticipantDto
    {
        public Guid ParticipantID { get; set; }
        public Guid MeetingID { get; set; }
        public Guid? UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string InvitationStatus { get; set; } = "pending";
        public string ParticipantRole { get; set; } = "attendee";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Additional info
        public string? UserDisplayName { get; set; }
        public string? UserAvatarUrl { get; set; }
    }

    // ─── Create DTOs ────────────────────────────────────────────────────────
    public class CreateMeetingDto
    {
        [Required]
        public Guid TaskID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTimeOffset StartDateTime { get; set; }

        [Required]
        public DateTimeOffset EndDateTime { get; set; }

        [Url(ErrorMessage = "Invalid URL format")]
        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        [MaxLength(50)]
        public string? MeetingPlatform { get; set; }

        public bool IsRecurring { get; set; } = false;

        [MaxLength(100)]
        public string? RecurrencePattern { get; set; }

        public List<CreateMeetingParticipantDto>? Participants { get; set; }
    }

    public class CreateMeetingParticipantDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        public Guid? UserID { get; set; }

        [RegularExpression("^(organizer|attendee|optional)$")]
        public string? ParticipantRole { get; set; } = "attendee";
    }

    // ─── Update DTO ─────────────────────────────────────────────────────────
    public class UpdateMeetingDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset? StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        [Url(ErrorMessage = "Invalid URL format")]
        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        [MaxLength(50)]
        public string? MeetingPlatform { get; set; }

        [RegularExpression("^(scheduled|live|ended|cancelled)$")]
        public string? Status { get; set; }

        public bool? IsRecurring { get; set; }

        [MaxLength(100)]
        public string? RecurrencePattern { get; set; }
    }

    // ─── Filter & Statistics ────────────────────────────────────────────────
    public class MeetingFilterDto
    {
        public Guid? OrganizerUserID { get; set; }
        public Guid? TaskID { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? StartDateFrom { get; set; }
        public DateTimeOffset? StartDateTo { get; set; }
        public bool? IsRecurring { get; set; }
        public bool? IncludeParticipants { get; set; } = false;
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "StartDateTime";
        public bool SortDescending { get; set; } = false;
    }

    public class MeetingStatisticsDto
    {
        public int TotalMeetings { get; set; }
        public int ScheduledMeetings { get; set; }
        public int LiveMeetings { get; set; }
        public int EndedMeetings { get; set; }
        public int CancelledMeetings { get; set; }
        public int RecurringMeetings { get; set; }
        public int TotalParticipants { get; set; }
        public double AverageParticipantsPerMeeting { get; set; }
        public Dictionary<string, int> MeetingsByPlatform { get; set; } = new();
        public Dictionary<string, int> MeetingsByDay { get; set; } = new();
    }
}