// Application/DTOs/ChatDtos.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    // ─── Connection DTOs ────────────────────────────────────────────────────
    public class UserConnectionDto
    {
        public Guid ConnectionID { get; set; }
        public Guid RequesterUserID { get; set; }
        public Guid RecipientUserID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Additional info
        public string? RequesterDisplayName { get; set; }
        public string? RequesterAvatarUrl { get; set; }
        public string? RecipientDisplayName { get; set; }
        public string? RecipientAvatarUrl { get; set; }
    }

    public class CreateConnectionRequestDto
    {
        [Required]
        public Guid RecipientUserID { get; set; }
    }

    public class UpdateConnectionStatusDto
    {
        [Required]
        [RegularExpression("^(accepted|declined|blocked)$")]
        public string Status { get; set; } = string.Empty;
    }

    // ─── Conversation DTOs ──────────────────────────────────────────────────
    public class ConversationDto
    {
        public Guid ConversationID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Related data
        public List<ConversationParticipantDto> Participants { get; set; } = new();
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ConversationParticipantDto
    {
        public Guid ParticipantID { get; set; }
        public Guid ConversationID { get; set; }
        public Guid UserID { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
        public DateTimeOffset? LeftAt { get; set; }

        // Additional info
        public string? UserDisplayName { get; set; }
        public string? UserAvatarUrl { get; set; }
        public bool IsOnline { get; set; }
    }

    public class CreateConversationDto
    {
        [Required]
        public List<Guid> ParticipantUserIDs { get; set; } = new();

        [MaxLength(255)]
        public string? Name { get; set; }
    }

    // ─── Message DTOs ───────────────────────────────────────────────────────
    public class MessageDto
    {
        public Guid MessageID { get; set; }
        public Guid ConversationID { get; set; }
        public Guid SenderUserID { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset? ReadAt { get; set; }

        // Additional info
        public string? SenderDisplayName { get; set; }
        public string? SenderAvatarUrl { get; set; }
    }

    public class SendMessageDto
    {
        [Required]
        public Guid ConversationID { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;
    }

    public class MarkReadDto
    {
        [Required]
        public Guid ConversationID { get; set; }
    }

    // ─── Filter DTOs ────────────────────────────────────────────────────────
    public class MessageFilterDto
    {
        public Guid ConversationID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ConversationFilterDto
    {
        public Guid? UserID { get; set; }
        public string? Type { get; set; }
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}