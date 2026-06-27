// Application/Services/ChatService.cs
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
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ApplicationDbContext context, IMapper mapper, ILogger<ChatService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════
        // CONNECTIONS
        // ═══════════════════════════════════════════════════

        public async Task<UserConnectionDto> SendConnectionRequestAsync(Guid requesterUserID, CreateConnectionRequestDto requestDto)
        {
            // Check if users are already connected
            var existingConnection = await _context.UserConnections
                .FirstOrDefaultAsync(uc =>
                    (uc.RequesterUserID == requesterUserID && uc.RecipientUserID == requestDto.RecipientUserID) ||
                    (uc.RequesterUserID == requestDto.RecipientUserID && uc.RecipientUserID == requesterUserID));

            if (existingConnection != null)
            {
                if (existingConnection.Status == "accepted")
                    throw new InvalidOperationException("Users are already connected");
                if (existingConnection.Status == "pending")
                    throw new InvalidOperationException("A connection request is already pending");
                if (existingConnection.Status == "blocked")
                    throw new InvalidOperationException("Cannot send request to a blocked user");
            }

            var connection = new UserConnection
            {
                RequesterUserID = requesterUserID,
                RecipientUserID = requestDto.RecipientUserID,
                Status = "pending"
            };

            _context.UserConnections.Add(connection);
            await _context.SaveChangesAsync();

            return _mapper.Map<UserConnectionDto>(connection);
        }

        public async Task<UserConnectionDto> UpdateConnectionStatusAsync(Guid connectionID, Guid recipientUserID, UpdateConnectionStatusDto statusDto)
        {
            var connection = await _context.UserConnections
                .Include(uc => uc.Requester)
                .Include(uc => uc.Recipient)
                .FirstOrDefaultAsync(uc => uc.ConnectionID == connectionID);

            if (connection == null)
                throw new KeyNotFoundException("Connection request not found");

            if (connection.RecipientUserID != recipientUserID)
                throw new UnauthorizedAccessException("Only the recipient can update the connection status");

            // Create a conversation when connection is accepted
            if (statusDto.Status == "accepted" && connection.Status != "accepted")
            {
                var conversation = new Conversation
                {
                    Type = "direct",
                    Participants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant { UserID = connection.RequesterUserID },
                        new ConversationParticipant { UserID = connection.RecipientUserID }
                    }
                };
                _context.Conversations.Add(conversation);
            }

            connection.Status = statusDto.Status;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return _mapper.Map<UserConnectionDto>(connection);
        }

        public async Task<IEnumerable<UserConnectionDto>> GetPendingRequestsAsync(Guid userID)
        {
            var connections = await _context.UserConnections
                .Include(uc => uc.Requester)
                .Include(uc => uc.Recipient)
                .Where(uc => uc.RecipientUserID == userID && uc.Status == "pending")
                .OrderByDescending(uc => uc.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserConnectionDto>>(connections);
        }

        public async Task<IEnumerable<UserConnectionDto>> GetConnectionsAsync(Guid userID, string? status = null)
        {
            var query = _context.UserConnections
                .Include(uc => uc.Requester)
                .Include(uc => uc.Recipient)
                .Where(uc => (uc.RequesterUserID == userID || uc.RecipientUserID == userID));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(uc => uc.Status == status);

            var connections = await query.OrderByDescending(uc => uc.UpdatedAt).ToListAsync();
            return _mapper.Map<IEnumerable<UserConnectionDto>>(connections);
        }

        public async Task<bool> AreUsersConnectedAsync(Guid userID1, Guid userID2)
        {
            return await _context.UserConnections
                .AnyAsync(uc =>
                    uc.Status == "accepted" &&
                    ((uc.RequesterUserID == userID1 && uc.RecipientUserID == userID2) ||
                     (uc.RequesterUserID == userID2 && uc.RecipientUserID == userID1)));
        }

        // ═══════════════════════════════════════════════════
        // CONVERSATIONS
        // ═══════════════════════════════════════════════════

        public async Task<ConversationDto> CreateConversationAsync(Guid creatorUserID, CreateConversationDto createDto)
        {
            // Ensure creator is included in participants
            if (!createDto.ParticipantUserIDs.Contains(creatorUserID))
                createDto.ParticipantUserIDs.Add(creatorUserID);

            var isGroup = createDto.ParticipantUserIDs.Count > 2;
            // For direct chats, check if conversation already exists
            if (!isGroup && createDto.ParticipantUserIDs.Count == 2)
            {
                var otherUserID = createDto.ParticipantUserIDs.First(u => u != creatorUserID);
                var existingConversation = await FindDirectConversationAsync(creatorUserID, otherUserID);
                if (existingConversation != null)
                    return _mapper.Map<ConversationDto>(existingConversation);
            }

            var conversation = new Conversation
            {
                Type = isGroup ? "group" : "direct",
                Name = createDto.Name,
                Participants = createDto.ParticipantUserIDs.Select(userID => new ConversationParticipant
                {
                    UserID = userID
                }).ToList()
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return _mapper.Map<ConversationDto>(conversation);
        }

        private async Task<Conversation?> FindDirectConversationAsync(Guid userID1, Guid userID2)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Type == "direct")
                .FirstOrDefaultAsync(c =>
                    c.Participants.Any(p => p.UserID == userID1) &&
                    c.Participants.Any(p => p.UserID == userID2) &&
                    c.Participants.Count == 2);
        }

        public async Task<ConversationDto?> GetConversationByIdAsync(Guid conversationID, Guid requestingUserID)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .FirstOrDefaultAsync(c => c.ConversationID == conversationID);

            if (conversation == null) return null;

            // Verify requesting user is a participant
            if (!conversation.Participants.Any(p => p.UserID == requestingUserID && p.LeftAt == null))
                throw new UnauthorizedAccessException("You are not a participant in this conversation");

            var dto = _mapper.Map<ConversationDto>(conversation);

            // Get unread count
            dto.UnreadCount = await _context.Messages
                .CountAsync(m => m.ConversationID == conversationID && m.SenderUserID != requestingUserID && !m.IsRead);

            return dto;
        }

        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userID, ConversationFilterDto? filter = null)
        {
            var query = _context.Conversations
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserID == userID && p.LeftAt == null));

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Type))
                    query = query.Where(c => c.Type == filter.Type);
            }

            var conversations = await query.OrderByDescending(c => c.UpdatedAt).ToListAsync();

            var dtos = _mapper.Map<IEnumerable<ConversationDto>>(conversations);

            // Fill unread counts
            foreach (var dto in dtos)
            {
                dto.UnreadCount = await _context.Messages
                    .CountAsync(m => m.ConversationID == dto.ConversationID && m.SenderUserID != userID && !m.IsRead);
            }

            return dtos;
        }

        public async Task<bool> AddParticipantToConversationAsync(Guid conversationID, Guid userID, Guid requestingUserID)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.ConversationID == conversationID);

            if (conversation == null || conversation.Type != "group") return false;

            if (!conversation.Participants.Any(p => p.UserID == requestingUserID))
                throw new UnauthorizedAccessException("Only participants can add members");

            if (conversation.Participants.Any(p => p.UserID == userID)) return false;
            // Rejoin if previously left
            var existingParticipant = conversation.Participants.FirstOrDefault(p => p.UserID == userID && p.LeftAt != null);
            if (existingParticipant != null)
            {
                existingParticipant.LeftAt = null;
            }
            else
            {
                conversation.Participants.Add(new ConversationParticipant { UserID = userID });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveParticipantFromConversationAsync(Guid conversationID, Guid userID, Guid requestingUserID)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationID == conversationID && p.UserID == userID && p.LeftAt == null);

            if (participant == null) return false;

            participant.LeftAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ═══════════════════════════════════════════════════
        // MESSAGES
        // ═══════════════════════════════════════════════════

        public async Task<MessageDto> SendMessageAsync(Guid senderUserID, SendMessageDto sendDto)
        {
            // Verify sender is a participant
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationID == sendDto.ConversationID && p.UserID == senderUserID && p.LeftAt == null);

            if (!isParticipant)
                throw new UnauthorizedAccessException("You are not a participant in this conversation");

            var message = new Message
            {
                ConversationID = sendDto.ConversationID,
                SenderUserID = senderUserID,
                Content = sendDto.Content
            };

            _context.Messages.Add(message);

            // Update conversation timestamp
            var conversation = await _context.Conversations.FindAsync(sendDto.ConversationID);
            if (conversation != null)
                conversation.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<MessageDto>(message);
        }

        public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(Guid conversationID, Guid requestingUserID, MessageFilterDto? filter = null)
        {
            // Verify user is a participant
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationID == conversationID && p.UserID == requestingUserID);

            if (!isParticipant)
                throw new UnauthorizedAccessException("You are not a participant in this conversation");

            var query = _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationID == conversationID)
                .OrderByDescending(m => m.SentAt);

            if (filter != null)
            {
                query = (IOrderedQueryable<Message>)query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize);
            }

            var messages = await query.ToListAsync();
            return _mapper.Map<IEnumerable<MessageDto>>(messages.OrderBy(m => m.SentAt));
        }

        public async Task<bool> MarkMessagesAsReadAsync(Guid conversationID, Guid userID)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationID == conversationID && m.SenderUserID != userID && !m.IsRead)
                .ToListAsync();

            if (!unreadMessages.Any()) return false;

            var now = DateTimeOffset.UtcNow;
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid userID)
        {
            var userConversationIDs = await _context.ConversationParticipants
                .Where(p => p.UserID == userID && p.LeftAt == null)
                .Select(p => p.ConversationID)
                .ToListAsync();

            return await _context.Messages
                .CountAsync(m => userConversationIDs.Contains(m.ConversationID) && m.SenderUserID != userID && !m.IsRead);
        }
    }
}