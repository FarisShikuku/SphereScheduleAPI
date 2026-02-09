using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Category : BaseEntity
    {
        public Guid CategoryId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; } = "custom"; // system or custom
        public string Description { get; set; }
        public string ColorCode { get; set; } = "#4CAF50";
        public string IconName { get; set; }
        public int CategoryOrder { get; set; } = 0;
        public bool IsDefault { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
    }
}