// Domain/Entities/Event.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Event : BaseEntity
    {
        public Guid EventID { get; set; } = Guid.NewGuid();

        public Guid UserID { get; set; }

        public Guid? CategoryID { get; set; }

        /// <summary>
        /// Optional link to an auto-generated Task for this event.
        /// </summary>
        public Guid? TaskID { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Format description or link (e.g., venue link, online platform URL).
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Planning notes or file path to an uploaded PDF plan.
        /// </summary>
        public string? PlanningNotes { get; set; }

        public DateTimeOffset? StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        public string Status { get; set; } = "planned";
        // planned, ongoing, completed, cancelled

        public bool IsRecurring { get; set; } = false;

        public string? RecurrencePattern { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual EventCategory? Category { get; set; }
        public virtual TaskEntity? Task { get; set; }
        public virtual ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}