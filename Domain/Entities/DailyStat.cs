using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class DailyStat : BaseEntity
    {
        public Guid StatId { get; set; } = Guid.NewGuid();
        public Guid UserID { get; set; }
        public DateTime StatDate { get; set; }

        public int TotalTasks { get; set; } = 0;
        public int CompletedTasks { get; set; } = 0;
        public int IncompleteTasks { get; set; } = 0;
        public int OverdueTasks { get; set; } = 0;
        public int PersonalTasks { get; set; } = 0;
        public int JobTasks { get; set; } = 0;
        public int UnspecifiedTasks { get; set; } = 0;
        public int AppointmentTasks { get; set; } = 0;

        public int TotalAppointments { get; set; } = 0;
        public int CompletedAppointments { get; set; } = 0;
        public int CancelledAppointments { get; set; } = 0;

        public decimal? ProductivityScore { get; set; }
        public int CurrentStreakDays { get; set; } = 0;

        public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
        // REMOVED: CreatedAt, UpdatedAt, IsDeleted (from BaseEntity)

        public virtual User User { get; set; }
    }
}