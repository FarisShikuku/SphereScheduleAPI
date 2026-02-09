using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
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

        // Authentication
        public bool EmailVerified { get; set; } = false;
        public bool TwoFactorEnabled { get; set; } = false;
        public bool LockoutEnabled { get; set; } = false;
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; } = 0;

        // Account Info
        public string AccountType { get; set; } = "free"; // free, premium, enterprise, admin
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        // External IDs
        public string GoogleId { get; set; }
        public string MicrosoftId { get; set; }
        public string FacebookId { get; set; }

        // Preferences (JSON format)
        public string Preferences { get; set; } = @"{
            ""theme"": ""light"",
            ""timezone"": ""UTC"",
            ""language"": ""en"",
            ""notificationSettings"": {
                ""email"": true,
                ""push"": true,
                ""sms"": false
            },
            ""workHours"": {
                ""start"": ""09:00"",
                ""end"": ""17:00""
            },
            ""weekStartDay"": 1
        }";

        // Status
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset? LastActivityAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}