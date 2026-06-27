using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid UserID { get; set; } = Guid.NewGuid();
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public bool EmailVerified { get; set; } = false;
        public bool TwoFactorEnabled { get; set; } = false;
        public bool LockoutEnabled { get; set; } = false;
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; } = 0;

        public string AccountType { get; set; } = "free";
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        public string GoogleId { get; set; }
        public string MicrosoftId { get; set; }
        public string FacebookId { get; set; }

        public string Preferences { get; set; } = @"{...}";

        public bool IsActive { get; set; } = true;
        // REMOVED: IsDeleted, CreatedAt, UpdatedAt, DeletedAt (from BaseEntity)
        // REMOVED: LastLoginAt, LastActivityAt (these are in your database but not in BaseEntity - they stay)

        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset? LastActivityAt { get; set; }

        public virtual ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        // NEW navigation properties
        public virtual ICollection<Meeting> OrganizedMeetings { get; set; } = new List<Meeting>();
        public virtual ICollection<MeetingParticipant> MeetingParticipations { get; set; } = new List<MeetingParticipant>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
        public virtual ICollection<EventCategory> CustomEventCategories { get; set; } = new List<EventCategory>();
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // Chat-related navigation
        public virtual ICollection<UserConnection> SentConnectionRequests { get; set; } = new List<UserConnection>();
        public virtual ICollection<UserConnection> ReceivedConnectionRequests { get; set; } = new List<UserConnection>();
        public virtual ICollection<ConversationParticipant> ConversationParticipations { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}