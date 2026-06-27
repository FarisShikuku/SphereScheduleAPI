// Application/DTOs/EventDtos.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    // ─── Response DTOs ──────────────────────────────────────────────────────
    public class EventDto
    {
        public Guid EventID { get; set; }
        public Guid UserID { get; set; }
        public Guid? CategoryID { get; set; }
        public Guid? TaskID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Format { get; set; }
        public string? PlanningNotes { get; set; }
        public DateTimeOffset? StartDateTime { get; set; }
        public DateTimeOffset? EndDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Related data
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public string? UserDisplayName { get; set; }
        public int ParticipantCount { get; set; }
        public int ReminderCount { get; set; }
        public List<EventParticipantDto> Participants { get; set; } = new();
    }

    public class EventParticipantDto
    {
        public Guid ParticipantID { get; set; }
        public Guid EventID { get; set; }
        public Guid? UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string InvitationStatus { get; set; } = "pending";
        public string ParticipantRole { get; set; } = "attendee";
    }

    public class EventCategoryDto
    {
        public Guid CategoryID { get; set; }
        public Guid? UserID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string ColorCode { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public int EventCount { get; set; }
    }

    // ─── Create DTOs ────────────────────────────────────────────────────────
    public class CreateEventDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public Guid? CategoryID { get; set; }

        [MaxLength(500)]
        public string? Format { get; set; }

        public string? PlanningNotes { get; set; }

        public DateTimeOffset? StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        public bool IsRecurring { get; set; } = false;

        [MaxLength(100)]
        public string? RecurrencePattern { get; set; }

        public bool CreateLinkedTask { get; set; } = false;

        public List<CreateEventParticipantDto>? Participants { get; set; }
    }

    public class CreateEventParticipantDto
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

    public class CreateEventCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Icon { get; set; }

        [RegularExpression("^#([A-Fa-f0-9]{6})$")]
        public string? ColorCode { get; set; } = "#7C6CF8";
    }

    // ─── Update DTO ─────────────────────────────────────────────────────────
    public class UpdateEventDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        public Guid? CategoryID { get; set; }

        [MaxLength(500)]
        public string? Format { get; set; }

        public string? PlanningNotes { get; set; }

        public DateTimeOffset? StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        [RegularExpression("^(planned|ongoing|completed|cancelled)$")]
        public string? Status { get; set; }

        public bool? IsRecurring { get; set; }

        [MaxLength(100)]
        public string? RecurrencePattern { get; set; }
    }

    // ─── Filter & Statistics ────────────────────────────────────────────────
    public class EventFilterDto
    {
        public Guid? UserID { get; set; }
        public Guid? CategoryID { get; set; }
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

    public class EventStatisticsDto
    {
        public int TotalEvents { get; set; }
        public int PlannedEvents { get; set; }
        public int OngoingEvents { get; set; }
        public int CompletedEvents { get; set; }
        public int CancelledEvents { get; set; }
        public int TotalParticipants { get; set; }
        public Dictionary<string, int> EventsByCategory { get; set; } = new();
        public Dictionary<string, int> EventsByStatus { get; set; } = new();
    }
}