// Application/Interfaces/INotificationService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userID, int pageNumber = 1, int pageSize = 20);
        Task<UserNotificationSummaryDto> GetNotificationSummaryAsync(Guid userID);
        Task<int> GetUnreadCountAsync(Guid userID);
        Task<bool> MarkAsReadAsync(Guid notificationID, Guid userID);
        Task<bool> MarkAllAsReadAsync(Guid userID);
        Task<bool> MarkMultipleAsReadAsync(Guid userID, List<Guid> notificationIDs);

        // System-triggered notifications (called by other services)
        Task<NotificationDto> CreateNotificationAsync(Guid userID, string type, string title, string? message = null, string? entityType = null, Guid? entityID = null);
        Task<NotificationDto> SendTaskReminderNotificationAsync(Guid userID, Guid taskID, string taskTitle);
        Task<NotificationDto> SendMeetingInvitationNotificationAsync(Guid userID, Guid meetingID, string meetingTitle);
        Task<NotificationDto> SendEventUpdateNotificationAsync(Guid userID, Guid eventID, string eventName, string updateType);
        Task<NotificationDto> SendConnectionRequestNotificationAsync(Guid userID, Guid requesterID, string requesterName);
        Task<NotificationDto> SendNewMessageNotificationAsync(Guid userID, Guid conversationID, string senderName);
    }
}