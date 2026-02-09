using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class AppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AppointmentType { get; set; } = "general";
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public bool AllDayEvent { get; set; }
        public string? Location { get; set; }
        public bool IsVirtual { get; set; }
        public string? MeetingLink { get; set; }
        public string? MeetingPlatform { get; set; }
        public string Status { get; set; } = "scheduled";
        public int ReminderMinutesBefore { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public string CalendarColor { get; set; } = "#2196F3";
        public string? ExternalEventId { get; set; }
        public string ExternalSyncStatus { get; set; } = "not_synced";
        public string? Notes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int ParticipantCount { get; set; }
        public int ReminderCount { get; set; }
    }

    public class CreateAppointmentDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MaxLength(30)]
        [RegularExpression("^(general|doctor|business|personal)$", ErrorMessage = "Invalid appointment type")]
        public string AppointmentType { get; set; } = "general";

        [Required]
        [FutureDate(ErrorMessage = "StartDateTime must be in the future")]
        public DateTimeOffset StartDateTime { get; set; }

        [Required]
        [DateAfter("StartDateTime", ErrorMessage = "EndDateTime must be after StartDateTime")]
        public DateTimeOffset EndDateTime { get; set; }

        public bool AllDayEvent { get; set; } = false;

        [MaxLength(500)]
        public string? Location { get; set; }

        public bool IsVirtual { get; set; } = false;

        [Url(ErrorMessage = "Invalid URL format for MeetingLink")]
        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        [MaxLength(50)]
        public string? MeetingPlatform { get; set; }

        public int ReminderMinutesBefore { get; set; } = 15;

        public bool IsRecurring { get; set; } = false;

        [MaxLength(100)]
        public string? RecurrencePattern { get; set; }

        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid hex color format")]
        [MaxLength(7)]
        public string CalendarColor { get; set; } = "#2196F3";

        [MaxLength(255)]
        public string? ExternalEventId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Participants for the appointment
        public List<CreateParticipantDto>? Participants { get; set; }
    }

    public class UpdateAppointmentDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [MaxLength(30)]
        [RegularExpression("^(general|doctor|business|personal)$", ErrorMessage = "Invalid appointment type")]
        public string? AppointmentType { get; set; }

        [FutureDate(ErrorMessage = "StartDateTime must be in the future")]
        public DateTimeOffset? StartDateTime { get; set; }

        [DateAfter("StartDateTime", ErrorMessage = "EndDateTime must be after StartDateTime")]
        public DateTimeOffset? EndDateTime { get; set; }

        public bool? AllDayEvent { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        public bool? IsVirtual { get; set; }

        [Url(ErrorMessage = "Invalid URL format for MeetingLink")]
        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        [MaxLength(50)]
        public string? MeetingPlatform { get; set; }

        [MaxLength(20)]
        [RegularExpression("^(scheduled|confirmed|cancelled|completed|rescheduled)$", ErrorMessage = "Invalid status")]
        public string? Status { get; set; }

        [Range(0, 1440, ErrorMessage = "ReminderMinutesBefore must be between 0 and 1440")]
        public int? ReminderMinutesBefore { get; set; }

        public bool? IsRecurring { get; set; }

        [MaxLength(100)]
        public string? RecurrencePattern { get; set; }

        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid hex color format")]
        [MaxLength(7)]
        public string? CalendarColor { get; set; }

        [MaxLength(255)]
        public string? ExternalEventId { get; set; }

        [MaxLength(20)]
        [RegularExpression("^(not_synced|synced|sync_failed)$", ErrorMessage = "Invalid sync status")]
        public string? ExternalSyncStatus { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class AppointmentFilterDto
    {
        public Guid? UserId { get; set; }
        public string? AppointmentType { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? StartDateFrom { get; set; }
        public DateTimeOffset? StartDateTo { get; set; }
        public DateTimeOffset? EndDateFrom { get; set; }
        public DateTimeOffset? EndDateTo { get; set; }
        public bool? IsVirtual { get; set; }
        public bool? IsRecurring { get; set; }
        public bool? IncludeDeleted { get; set; } = false;
        public bool? IncludeParticipants { get; set; } = false;
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "StartDateTime";
        public bool SortDescending { get; set; } = false;
    }

    public class AppointmentStatisticsDto
    {
        public int TotalAppointments { get; set; }
        public int ScheduledAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int VirtualAppointments { get; set; }
        public int InPersonAppointments { get; set; }
        public Dictionary<string, int> AppointmentsByType { get; set; } = new();
        public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();
        public Dictionary<string, int> MonthlyTrend { get; set; } = new();
    }

    // Custom validation attribute for date comparison
    public class DateAfterAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateAfterAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var currentValue = (DateTimeOffset)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
            {
                return new ValidationResult($"Unknown property: {_comparisonProperty}");
            }

            var comparisonValue = property.GetValue(validationContext.ObjectInstance) as DateTimeOffset?;

            if (!comparisonValue.HasValue)
            {
                return ValidationResult.Success;
            }

            return currentValue > comparisonValue.Value
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be after {_comparisonProperty}");
        }
    }
}