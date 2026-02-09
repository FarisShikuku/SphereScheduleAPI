namespace SphereScheduleAPI.Domain.Entities
{
    public abstract class BaseEntity
    {
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}