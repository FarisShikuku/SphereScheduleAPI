// Domain/Entities/Message.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class Message
    {
        public Guid MessageID { get; set; } = Guid.NewGuid();

        public Guid ConversationID { get; set; }

        public Guid SenderUserID { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTimeOffset? ReadAt { get; set; }

        // Navigation properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
    }
}