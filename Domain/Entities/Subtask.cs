using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Subtask : BaseEntity
    {
        public Guid SubtaskId { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "pending"; // pending, in_progress, completed, cancelled
        public string Priority { get; set; } = "medium"; // low, medium, high
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public int SubtaskOrder { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual TaskEntity Task { get; set; }
    }
}