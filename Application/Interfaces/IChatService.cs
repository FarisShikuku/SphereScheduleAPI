// Application/Interfaces/IChatService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IChatService
    {
        // Connections
        Task<UserConnectionDto> SendConnectionRequestAsync(Guid requesterUserID, CreateConnectionRequestDto requestDto);
        Task<UserConnectionDto> UpdateConnectionStatusAsync(Guid connectionID, Guid recipientUserID, UpdateConnectionStatusDto statusDto);
        Task<IEnumerable<UserConnectionDto>> GetPendingRequestsAsync(Guid userID);
        Task<IEnumerable<UserConnectionDto>> GetConnectionsAsync(Guid userID, string? status = null);
        Task<bool> AreUsersConnectedAsync(Guid userID1, Guid userID2);

        // Conversations
        Task<ConversationDto> CreateConversationAsync(Guid creatorUserID, CreateConversationDto createDto);
        Task<ConversationDto?> GetConversationByIdAsync(Guid conversationID, Guid requestingUserID);
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userID, ConversationFilterDto? filter = null);
        Task<bool> AddParticipantToConversationAsync(Guid conversationID, Guid userID, Guid requestingUserID);
        Task<bool> RemoveParticipantFromConversationAsync(Guid conversationID, Guid userID, Guid requestingUserID);

        // Messages
        Task<MessageDto> SendMessageAsync(Guid senderUserID, SendMessageDto sendDto);
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(Guid conversationID, Guid requestingUserID, MessageFilterDto? filter = null);
        Task<bool> MarkMessagesAsReadAsync(Guid conversationID, Guid userID);
        Task<int> GetUnreadMessageCountAsync(Guid userID);
    }
}