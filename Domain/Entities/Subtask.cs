using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Subtask : BaseEntity
    {
        public Guid SubTaskID { get; set; } = Guid.NewGuid();
        public Guid TaskID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "pending";
        public string Priority { get; set; } = "medium";
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public int SubtaskOrder { get; set; } = 0;
        // REMOVED: CreatedAt, UpdatedAt, IsDeleted (from BaseEntity)

        public virtual TaskEntity Task { get; set; }
    }
}