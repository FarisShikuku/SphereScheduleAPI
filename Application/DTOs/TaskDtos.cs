using System;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class TaskDto
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string TaskType { get; set; }
        public string PriorityLevel { get; set; }
        public string Status { get; set; }
        public int CompletionPercentage { get; set; }
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public DateTime? StartDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string LocationName { get; set; }
        public string LocationAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? EstimatedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public int TimeSpentMinutes { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrenceRule { get; set; }
        public string Tags { get; set; }
        public string Notes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int SubtaskCount { get; set; }
        public int CompletedSubtasks { get; set; }
        public int ReminderCount { get; set; }
        public string DueStatus { get; set; }
        public int? DaysUntilDue { get; set; }
    }

    public class CreateTaskDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        public string Category { get; set; } = "unspecified";

        public string TaskType { get; set; } = "general";

        public string PriorityLevel { get; set; } = "medium";

        public DateTime? DueDate { get; set; }

        public TimeSpan? DueTime { get; set; }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Range(0, 10000)]
        public int? EstimatedDurationMinutes { get; set; }

        [StringLength(500)]
        public string LocationName { get; set; }

        [StringLength(500)]
        public string LocationAddress { get; set; }

        public bool IsRecurring { get; set; }

        [StringLength(500)]
        public string RecurrenceRule { get; set; }

        [StringLength(500)]
        public string Tags { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }
    }

    public class UpdateTaskDto
    {
        [StringLength(255, MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        public string Category { get; set; }

        public string TaskType { get; set; }

        public string PriorityLevel { get; set; }

        public string Status { get; set; }

        [Range(0, 100)]
        public int? CompletionPercentage { get; set; }

        public DateTime? DueDate { get; set; }

        public TimeSpan? DueTime { get; set; }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Range(0, 10000)]
        public int? EstimatedDurationMinutes { get; set; }

        [Range(0, 10000)]
        public int? ActualDurationMinutes { get; set; }

        [Range(0, 100000)]
        public int? TimeSpentMinutes { get; set; }

        [StringLength(500)]
        public string LocationName { get; set; }

        [StringLength(500)]
        public string LocationAddress { get; set; }

        public bool? IsRecurring { get; set; }

        [StringLength(500)]
        public string RecurrenceRule { get; set; }

        [StringLength(500)]
        public string Tags { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }
    }
}