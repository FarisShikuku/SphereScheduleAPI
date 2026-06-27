// Domain/Entities/ConversationParticipant.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class ConversationParticipant
    {
        public Guid ParticipantID { get; set; } = Guid.NewGuid();

        public Guid ConversationID { get; set; }

        public Guid UserID { get; set; }

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LeftAt { get; set; }

        // Navigation properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}