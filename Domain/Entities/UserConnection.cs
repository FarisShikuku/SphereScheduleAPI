// Domain/Entities/UserConnection.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    public class UserConnection
    {
        // Does NOT inherit BaseEntity (has its own CreatedAt/UpdatedAt pattern)

        public Guid ConnectionID { get; set; } = Guid.NewGuid();

        public Guid RequesterUserID { get; set; }

        public Guid RecipientUserID { get; set; }

        public string Status { get; set; } = "pending";
        // pending, accepted, declined, blocked

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User Requester { get; set; } = null!;
        public virtual User Recipient { get; set; } = null!;
    }
}