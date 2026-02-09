using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class ReminderDto
    {
        public Guid ReminderId { get; set; }
        public Guid UserId { get; set; }
        public Guid? TaskId { get; set; }
        public Guid? AppointmentId { get; set; }
        public string ReminderType { get; set; } = "general";
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTimeOffset ReminderDateTime { get; set; }
        public bool NotifyViaEmail { get; set; } = true;
        public bool NotifyViaPush { get; set; } = true;
        public string Status { get; set; } = "pending";
        public bool IsRecurring { get; set; } = false;
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class CreateReminderDto
    {
        [Required]
        public Guid UserId { get; set; }

        public Guid? TaskId { get; set; }

        public Guid? AppointmentId { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression("^(task|appointment|general)$", ErrorMessage = "ReminderType must be 'task', 'appointment', or 'general'")]
        public string ReminderType { get; set; } = "general";

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }

        [Required]
        [FutureDate(ErrorMessage = "ReminderDateTime must be in the future")]
        public DateTimeOffset ReminderDateTime { get; set; }

        public bool NotifyViaEmail { get; set; } = true;

        public bool NotifyViaPush { get; set; } = true;

        public bool IsRecurring { get; set; } = false;
    }

    public class UpdateReminderDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        [FutureDate(ErrorMessage = "ReminderDateTime must be in the future")]
        public DateTimeOffset? ReminderDateTime { get; set; }

        public bool? NotifyViaEmail { get; set; }

        public bool? NotifyViaPush { get; set; }

        [MaxLength(20)]
        [RegularExpression("^(pending|triggered|sent|failed|cancelled)$", ErrorMessage = "Invalid status")]
        public string? Status { get; set; }

        public bool? IsRecurring { get; set; }
    }

    public class ReminderFilterDto
    {
        public Guid? UserId { get; set; }
        public Guid? TaskId { get; set; }
        public Guid? AppointmentId { get; set; }
        public string? ReminderType { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public bool? IsRecurring { get; set; }
        public bool? IncludeSent { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Custom validation attribute for future dates
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset > DateTimeOffset.UtcNow;
            }
            return false;
        }
    }
}