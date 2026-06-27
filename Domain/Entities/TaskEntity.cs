// Domain/Entities/TaskEntity.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class TaskEntity : BaseEntity
    {
        public Guid TaskID { get; set; } = Guid.NewGuid();
        public Guid UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TaskType { get; set; } = "general";
        public string PriorityLevel { get; set; } = "medium";
        public string Status { get; set; } = "pending";
        public int CompletionPercentage { get; set; } = 0;

        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public DateTime? StartDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public string? LocationName { get; set; }
        public string? LocationAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public int? EstimatedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public int TimeSpentMinutes { get; set; } = 0;

        public bool IsRecurring { get; set; } = false;
        public string? RecurrenceRule { get; set; }
        public Guid? ParentTaskID { get; set; }

        public string? ExternalID { get; set; }
        public string? ExternalSource { get; set; }
        public string ExternalSyncStatus { get; set; } = "not_synced";

        public string? Tags { get; set; }
        public string? Notes { get; set; }

        public Guid? CategoryID { get; set; }

        // ═══════════════════════════════════════════════════════════
        // Navigation Properties
        // ═══════════════════════════════════════════════════════════

        public virtual User User { get; set; } = null!;
        public virtual TaskEntity? ParentTask { get; set; }
        public virtual ICollection<TaskEntity> ChildTasks { get; set; } = new List<TaskEntity>();
        public virtual ICollection<Subtask> Subtasks { get; set; } = new List<Subtask>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
        public virtual Category? CategoryNavigation { get; set; }

        /// <summary>
        /// One-to-one relationship with Meeting.
        /// When TaskType is 'meeting', a Meeting entity can be linked.
        /// </summary>
        public virtual Meeting? Meeting { get; set; }

        /// <summary>
        /// Notes linked to this task.
        /// </summary>
        public virtual ICollection<Note> NotesList { get; set; } = new List<Note>();
    }
}