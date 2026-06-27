// Domain/Entities/Notification.cs
using System;

namespace SphereScheduleAPI.Domain.Entities
{
    /// <summary>
    /// Unified notification entity for all system notifications.
    /// Supports reminders, invitations, task due alerts, event updates, chat messages, etc.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Unique identifier for the notification.
        /// </summary>
        public Guid NotificationID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user who receives this notification.
        /// </summary>
        public Guid UserID { get; set; }

        /// <summary>
        /// Type of notification:
        /// 'reminder', 'invitation', 'task_due', 'event_update', 'chat_message', 'connection_request', etc.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Short title for the notification.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional detailed message body.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Whether the notification has been read by the user.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// The type of entity this notification relates to (e.g., "Task", "Meeting", "Event", "Conversation").
        /// Used for navigation when user clicks the notification.
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// The ID of the related entity.
        /// </summary>
        public Guid? EntityID { get; set; }

        /// <summary>
        /// When the notification was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // ═══════════════════════════════════════════════════════════
        // Navigation Properties
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// The user who receives this notification.
        /// </summary>
        public virtual User User { get; set; } = null!;
    }
}