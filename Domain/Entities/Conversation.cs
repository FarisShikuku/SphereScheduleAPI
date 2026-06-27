// Domain/Entities/Conversation.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Conversation
    {
        // Lightweight entity without soft-delete

        public Guid ConversationID { get; set; } = Guid.NewGuid();

        public string Type { get; set; } = "direct";
        // direct, group

        public string? Name { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}