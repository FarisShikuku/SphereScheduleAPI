// Application/Services/NotificationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;

namespace SphereScheduleAPI.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, IMapper mapper, ILogger<NotificationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userID, int pageNumber = 1, int pageSize = 20)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<UserNotificationSummaryDto> GetNotificationSummaryAsync(Guid userID)
        {
            var unreadCount = await _context.Notifications.CountAsync(n => n.UserID == userID && !n.IsRead);
            var recent = await _context.Notifications
                .Where(n => n.UserID == userID)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            return new UserNotificationSummaryDto
            {
                TotalUnread = unreadCount,
                RecentNotifications = _mapper.Map<List<NotificationDto>>(recent)
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userID)
        {
            return await _context.Notifications.CountAsync(n => n.UserID == userID && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationID, Guid userID)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == notificationID && n.UserID == userID);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userID)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserID == userID && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkMultipleAsReadAsync(Guid userID, List<Guid> notificationIDs)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID && notificationIDs.Contains(n.NotificationID))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ═══════════════════════════════════════════════════
        // SYSTEM-TRIGGERED NOTIFICATIONS
        // ═══════════════════════════════════════════════════

        public async Task<NotificationDto> CreateNotificationAsync(Guid userID, string type, string title, string? message = null, string? entityType = null, Guid? entityID = null)
        {
            var notification = new Notification
            {
                UserID = userID,
                Type = type,
                Title = title,
                Message = message,
                EntityType = entityType,
                EntityID = entityID
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<NotificationDto> SendTaskReminderNotificationAsync(Guid userID, Guid taskID, string taskTitle)
        {
            return await CreateNotificationAsync(userID, "task_due", "Task Reminder",
                $"Task \"{taskTitle}\" is due soon", "Task", taskID);
        }

        public async Task<NotificationDto> SendMeetingInvitationNotificationAsync(Guid userID, Guid meetingID, string meetingTitle)
        {
            return await CreateNotificationAsync(userID, "invitation", "Meeting Invitation",
                $"You've been invited to \"{meetingTitle}\"", "Meeting", meetingID);
        }

        public async Task<NotificationDto> SendEventUpdateNotificationAsync(Guid userID, Guid eventID, string eventName, string updateType)
        {
            return await CreateNotificationAsync(userID, "event_update", "Event Updated",
                $"Event \"{eventName}\" has been {updateType}", "Event", eventID);
        }

        public async Task<NotificationDto> SendConnectionRequestNotificationAsync(Guid userID, Guid requesterID, string requesterName)
        {
            return await CreateNotificationAsync(userID, "connection_request", "Connection Request",
                $"{requesterName} wants to connect with you", "User", requesterID);
        }

        public async Task<NotificationDto> SendNewMessageNotificationAsync(Guid userID, Guid conversationID, string senderName)
        {
            return await CreateNotificationAsync(userID, "chat_message", "New Message",
                $"You have a new message from {senderName}", "Conversation", conversationID);
        }
    }
}