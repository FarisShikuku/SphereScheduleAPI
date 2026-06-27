// Domain/Entities/EventParticipant.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class EventParticipant : BaseEntity
    {
        public Guid ParticipantID { get; set; } = Guid.NewGuid();

        public Guid EventID { get; set; }

        public Guid? UserID { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string InvitationStatus { get; set; } = "pending";
        // pending, sent, accepted, declined, tentative

        public string ParticipantRole { get; set; } = "attendee";
        // organizer, attendee, optional

        // Navigation properties
        public virtual Event Event { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}