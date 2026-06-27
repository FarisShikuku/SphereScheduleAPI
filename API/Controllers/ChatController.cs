// API/Controllers/ChatController.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        private Guid GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
            return userId;
        }

        // ═══════════════════════════════════════
        // CONNECTIONS
        // ═══════════════════════════════════════

        [HttpGet("connections")]
        [ProducesResponseType(typeof(IEnumerable<UserConnectionDto>), 200)]
        public async Task<IActionResult> GetConnections([FromQuery] string? status = null)
        {
            var userId = GetCurrentUserID();
            var connections = await _chatService.GetConnectionsAsync(userId, status);
            return Ok(connections);
        }

        [HttpGet("connections/pending")]
        [ProducesResponseType(typeof(IEnumerable<UserConnectionDto>), 200)]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetCurrentUserID();
            var requests = await _chatService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }

        [HttpPost("connections")]
        [ProducesResponseType(typeof(UserConnectionDto), 201)]
        public async Task<IActionResult> SendConnectionRequest([FromBody] CreateConnectionRequestDto requestDto)
        {
            try
            {
                var userId = GetCurrentUserID();
                var connection = await _chatService.SendConnectionRequestAsync(userId, requestDto);
                return CreatedAtAction(nameof(GetConnections), new { }, connection);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("connections/{connectionId}")]
        [ProducesResponseType(typeof(UserConnectionDto), 200)]
        public async Task<IActionResult> UpdateConnectionStatus(Guid connectionId, [FromBody] UpdateConnectionStatusDto statusDto)
        {
            try
            {
                var userId = GetCurrentUserID();
                var connection = await _chatService.UpdateConnectionStatusAsync(connectionId, userId, statusDto);
                return Ok(connection);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }

        // ═══════════════════════════════════════
        // CONVERSATIONS
        // ═══════════════════════════════════════

        [HttpGet("conversations")]
        [ProducesResponseType(typeof(IEnumerable<ConversationDto>), 200)]
        public async Task<IActionResult> GetConversations([FromQuery] ConversationFilterDto? filter = null)
        {
            var userId = GetCurrentUserID();
            var conversations = await _chatService.GetUserConversationsAsync(userId, filter);
            return Ok(conversations);
        }

        [HttpGet("conversations/{conversationId}")]
        [ProducesResponseType(typeof(ConversationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetConversation(Guid conversationId)
        {
            try
            {
                var userId = GetCurrentUserID();
                var conversation = await _chatService.GetConversationByIdAsync(conversationId, userId);
                if (conversation == null)
                    return NotFound(new { message = "Conversation not found" });

                return Ok(conversation);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("conversations")]
        [ProducesResponseType(typeof(ConversationDto), 201)]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto createDto)
        {
            var userId = GetCurrentUserID();
            var conversation = await _chatService.CreateConversationAsync(userId, createDto);
            return CreatedAtAction(nameof(GetConversation), new { conversationId = conversation.ConversationID }, conversation);
        }

        // ═══════════════════════════════════════
        // MESSAGES
        // ═══════════════════════════════════════

        [HttpGet("conversations/{conversationId}/messages")]
        [ProducesResponseType(typeof(IEnumerable<MessageDto>), 200)]
        public async Task<IActionResult> GetMessages(Guid conversationId, [FromQuery] MessageFilterDto? filter = null)
        {
            try
            {
                var userId = GetCurrentUserID();
                var messages = await _chatService.GetConversationMessagesAsync(conversationId, userId, filter);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("messages")]
        [ProducesResponseType(typeof(MessageDto), 201)]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto sendDto)
        {
            try
            {
                var userId = GetCurrentUserID();
                var message = await _chatService.SendMessageAsync(userId, sendDto);
                return CreatedAtAction(nameof(GetMessages), new { conversationId = message.ConversationID }, message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("conversations/{conversationId}/read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MarkAsRead(Guid conversationId)
        {
            var userId = GetCurrentUserID();
            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);
            return Ok(new { message = "Messages marked as read" });
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserID();
            var count = await _chatService.GetUnreadMessageCountAsync(userId);
            return Ok(new { unreadCount = count });
        }
    }
}