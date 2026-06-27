// API/Controllers/NotificationsController.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
            return userId;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<NotificationDto>), 200)]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserID();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
            return Ok(notifications);
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(UserNotificationSummaryDto), 200)]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetCurrentUserID();
            var summary = await _notificationService.GetNotificationSummaryAsync(userId);
            return Ok(summary);
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserID();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

        [HttpPost("{id}/read")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserID();
            var success = await _notificationService.MarkAsReadAsync(id, userId);
            if (!success)
                return NotFound(new { message = "Notification not found" });

            return Ok(new { message = "Notification marked as read" });
        }

        [HttpPost("read-all")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserID();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read" });
        }

        [HttpPost("read-multiple")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MarkMultipleAsRead([FromBody] MarkNotificationsReadDto readDto)
        {
            var userId = GetCurrentUserID();
            if (readDto.MarkAll)
            {
                await _notificationService.MarkAllAsReadAsync(userId);
            }
            else if (readDto.NotificationIDs != null && readDto.NotificationIDs.Count > 0)
            {
                await _notificationService.MarkMultipleAsReadAsync(userId, readDto.NotificationIDs);
            }
            return Ok(new { message = "Notifications marked as read" });
        }
    }
}