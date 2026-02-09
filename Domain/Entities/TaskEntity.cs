// Domain/Entities/TaskEntity.cs (rename to avoid conflict with System.Threading.Tasks.Task)
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class TaskEntity : BaseEntity
    {
        public Guid TaskId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } = "unspecified";
        public string TaskType { get; set; } = "general";
        public string PriorityLevel { get; set; } = "medium";
        public string Status { get; set; } = "pending";
        public int CompletionPercentage { get; set; } = 0;

        // Dates and Times
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public DateTime? StartDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        // Location
        public string LocationName { get; set; }
        public string LocationAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Time Tracking
        public int? EstimatedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public int TimeSpentMinutes { get; set; } = 0;

        // Recurrence
        public bool IsRecurring { get; set; }
        public string RecurrenceRule { get; set; }
        public Guid? ParentTaskId { get; set; }

        // External Integration
        public string ExternalId { get; set; }
        public string ExternalSource { get; set; }
        public string ExternalSyncStatus { get; set; } = "not_synced";

        // Tags and Notes
        public string Tags { get; set; }
        public string Notes { get; set; }

        // Status
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual TaskEntity ParentTask { get; set; }
        public virtual ICollection<TaskEntity> ChildTasks { get; set; } = new List<TaskEntity>();
        public virtual ICollection<Subtask> Subtasks { get; set; } = new List<Subtask>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}