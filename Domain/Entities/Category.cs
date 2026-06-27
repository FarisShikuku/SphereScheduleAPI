using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Category : BaseEntity
    {
        public Guid CategoryID { get; set; } = Guid.NewGuid();
        public Guid UserID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; } = "custom";
        public string Description { get; set; }
        public string ColorCode { get; set; } = "#4CAF50";
        public string IconName { get; set; }
        public int CategoryOrder { get; set; } = 0;
        public bool IsDefault { get; set; } = false;
        // REMOVED: IsDeleted, CreatedAt, UpdatedAt (from BaseEntity)

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
    }
}