// Domain/Entities/EventLog.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    /// <summary>
    /// Unified audit log for tracking all entity changes.
    /// Stored in the 'audit' schema.
    /// </summary>
    public class EventLog
    {
        public long LogID { get; set; }

        public Guid? UserID { get; set; }

        public string Action { get; set; } = string.Empty;
        // INSERT, UPDATE, DELETE, LOGIN, etc.

        public string? EntitySchema { get; set; }

        public string EntityName { get; set; } = string.Empty;

        public Guid? EntityID { get; set; }

        /// <summary>
        /// JSON snapshot of old values (for UPDATE/DELETE).
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON snapshot of new values (for INSERT/UPDATE).
        /// </summary>
        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string LogLevel { get; set; } = "Info";
        // Info, Warning, Error

        public string? Message { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
    }
}