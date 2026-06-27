// Domain/Entities/Meeting.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Meeting : BaseEntity
    {
        public Guid MeetingID { get; set; } = Guid.NewGuid();

        // One-to-one with Task (TaskID is unique)
        public Guid TaskID { get; set; }

        public Guid OrganizerUserID { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public string? MeetingLink { get; set; }

        public string? MeetingPlatform { get; set; }

        public bool IsRecurring { get; set; } = false;

        public string? RecurrencePattern { get; set; }

        public string Status { get; set; } = "scheduled";
        // scheduled, live, ended, cancelled

        // Navigation properties
        public virtual TaskEntity Task { get; set; } = null!;
        public virtual User Organizer { get; set; } = null!;
        public virtual ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}