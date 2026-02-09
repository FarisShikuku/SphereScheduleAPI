using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Appointment : BaseEntity
    {
        public Guid AppointmentId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AppointmentType { get; set; } = "general";
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public bool AllDayEvent { get; set; }
        public string Location { get; set; }
        public bool IsVirtual { get; set; }
        public string MeetingLink { get; set; }
        public string MeetingPlatform { get; set; }
        public string Status { get; set; } = "scheduled";
        public int ReminderMinutesBefore { get; set; } = 15;
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }
        public string CalendarColor { get; set; } = "#2196F3";
        public string ExternalEventId { get; set; }
        public string ExternalSyncStatus { get; set; } = "not_synced";
        public string Notes { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }  // Add this line

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}