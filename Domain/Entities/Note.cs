// Domain/Entities/Note.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Note : BaseEntity
    {
        public Guid NoteID { get; set; } = Guid.NewGuid();

        public Guid UserID { get; set; }

        public string? Title { get; set; }

        public string Content { get; set; } = string.Empty;

        // Optional links to related entities
        public Guid? TaskID { get; set; }
        public Guid? EventID { get; set; }
        public Guid? AppointmentID { get; set; }
        public Guid? MeetingID { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual TaskEntity? Task { get; set; }
        public virtual Event? Event { get; set; }
        public virtual Appointment? Appointment { get; set; }
        public virtual Meeting? Meeting { get; set; }
    }
}