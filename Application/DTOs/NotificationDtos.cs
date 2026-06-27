// Application/DTOs/NotificationDtos.cs
using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Application.DTOs
{
    public class NotificationDto
    {
        public Guid NotificationID { get; set; }
        public Guid UserID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityID { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UserNotificationSummaryDto
    {
        public int TotalUnread { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new();
    }

    public class MarkNotificationsReadDto
    {
        public List<Guid>? NotificationIDs { get; set; }
        public bool MarkAll { get; set; } = false;
    }
}