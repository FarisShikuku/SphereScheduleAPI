// Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Domain.Entities;
using TaskEntity = SphereScheduleAPI.Domain.Entities.TaskEntity;

namespace SphereScheduleAPI.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables in app schema
        public DbSet<User> Users { get; set; }
        public DbSet<TaskEntity> Tasks { get; set; }
        public DbSet<Subtask> Subtasks { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Category> Categories { get; set; }

        // NEW: Meeting tables
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingParticipant> MeetingParticipants { get; set; }

        // NEW: Event tables
        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }

        // NEW: Chat tables
        public DbSet<UserConnection> UserConnections { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }

        // NEW: Notes & Notifications
        public DbSet<Note> Notes { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Tables in audit schema
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // NEW: Audit EventLog
        public DbSet<EventLog> EventLogs { get; set; }

        // Tables in report schema
        public DbSet<DailyStat> DailyStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure default schema
            modelBuilder.HasDefaultSchema("app");

            // ================================================================
            // Users Table
            // ================================================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", "app");
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.UserID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasMaxLength(512);
                entity.Property(e => e.PasswordSalt).HasMaxLength(512);
                entity.Property(e => e.DisplayName).HasMaxLength(100);
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.DateOfBirth).HasColumnType("date");
                entity.Property(e => e.AccountType).HasMaxLength(20).HasDefaultValue("free");
                entity.Property(e => e.GoogleId).HasMaxLength(255);
                entity.Property(e => e.MicrosoftId).HasMaxLength(255);
                entity.Property(e => e.FacebookId).HasMaxLength(255);
                entity.Property(e => e.Preferences).HasColumnType("NVARCHAR(MAX)");

                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.EmailVerified).HasDefaultValue(false);
                entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
                entity.Property(e => e.LockoutEnabled).HasDefaultValue(false);
                entity.Property(e => e.AccessFailedCount).HasDefaultValue(0);
                entity.Property(e => e.LastLoginAt);
                entity.Property(e => e.LastActivityAt);

                // EXPLICITLY MAP BASEENTITY PROPERTIES
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Unique constraints
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[IsDeleted] = 0");
                entity.HasIndex(e => e.Username).IsUnique().HasFilter("[IsDeleted] = 0");

                // Indexes
                entity.HasIndex(e => e.AccountType);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDeleted);
                entity.HasIndex(e => e.EmailVerified);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastLoginAt);
                entity.HasIndex(e => new { e.Email, e.IsActive, e.IsDeleted });
            });

            // ================================================================
            // Categories Table
            // ================================================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories", "app");
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CategoryType).HasMaxLength(20).HasDefaultValue("custom");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ColorCode).HasMaxLength(7).HasDefaultValue("#4CAF50");
                entity.Property(e => e.IconName).HasMaxLength(50);
                entity.Property(e => e.CategoryOrder).HasDefaultValue(0);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Ignore(e => e.DeletedAt);
                // EXPLICITLY MAP BASEENTITY PROPERTIES
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                // entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key to User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Categories)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: User + CategoryName
                entity.HasIndex(e => new { e.UserID, e.CategoryName })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.CategoryType);
                entity.HasIndex(e => e.IsDefault);
                entity.HasIndex(e => e.CategoryOrder);
            });

            // ================================================================
            // Tasks Table
            // ================================================================
            modelBuilder.Entity<TaskEntity>(entity =>
            {
                entity.ToTable("Tasks", "app");
                entity.HasKey(e => e.TaskID);
                entity.Property(e => e.TaskID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.TaskType).HasMaxLength(30).HasDefaultValue("general");
                entity.Property(e => e.PriorityLevel).HasMaxLength(10).HasDefaultValue("medium");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.CompletionPercentage).HasDefaultValue(0);
                entity.Property(e => e.LocationName).HasMaxLength(255);
                entity.Property(e => e.LocationAddress).HasMaxLength(500);
                entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
                entity.Property(e => e.TimeSpentMinutes).HasDefaultValue(0);
                entity.Property(e => e.IsRecurring).HasDefaultValue(false);
                entity.Property(e => e.RecurrenceRule).HasMaxLength(500);
                entity.Property(e => e.ExternalID).HasMaxLength(255);
                entity.Property(e => e.ExternalSource).HasMaxLength(50);
                entity.Property(e => e.ExternalSyncStatus).HasMaxLength(20).HasDefaultValue("not_synced");
                entity.Property(e => e.Tags).HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(2000);

                // EXPLICITLY MAP BASEENTITY PROPERTIES
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraint for completion percentage
                entity.HasCheckConstraint("CK_Tasks_CompletionPercentage", "CompletionPercentage BETWEEN 0 AND 100");

                // Foreign key to User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Tasks)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing foreign key for ParentTask
                entity.HasOne(e => e.ParentTask)
                    .WithMany(t => t.ChildTasks)
                    .HasForeignKey(e => e.ParentTaskID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Foreign key to Category
                entity.HasOne(e => e.CategoryNavigation)
                    .WithMany(c => c.Tasks)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PriorityLevel);
                entity.HasIndex(e => e.CategoryID);
                entity.HasIndex(e => new { e.UserID, e.DueDate, e.Status })
                    .HasFilter("[IsDeleted] = 0");
                entity.HasIndex(e => new { e.UserID, e.CategoryID, e.Status })
                    .HasFilter("[IsDeleted] = 0");
            });

            // ================================================================
            // Subtasks Table
            // ================================================================
            modelBuilder.Entity<Subtask>(entity =>
            {
                entity.ToTable("Subtasks", "app");
                entity.HasKey(e => e.SubTaskID);
                entity.Property(e => e.SubTaskID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.Priority).HasMaxLength(10).HasDefaultValue("medium");
                entity.Property(e => e.SubtaskOrder).HasDefaultValue(0);
                entity.Ignore(e => e.DeletedAt);
                // EXPLICITLY MAP BASEENTITY PROPERTIES
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                // entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key to Task
                entity.HasOne(e => e.Task)
                    .WithMany(t => t.Subtasks)
                    .HasForeignKey(e => e.TaskID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.TaskID);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Priority);
                entity.HasIndex(e => e.DueDate);
            });

            // ================================================================
            // Appointments Table
            // ================================================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments", "app");
                entity.ToTable(tb => tb.HasTrigger("trg_Appointments_UpdateTimestamp"));
                entity.HasKey(e => e.AppointmentID);
                entity.Property(e => e.AppointmentID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.AppointmentType).HasMaxLength(30).HasDefaultValue("general");
                entity.Property(e => e.StartDateTime).IsRequired();
                entity.Property(e => e.EndDateTime).IsRequired();
                entity.Property(e => e.AllDayEvent).HasDefaultValue(false);
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.IsVirtual).HasDefaultValue(false);
                entity.Property(e => e.MeetingLink).HasMaxLength(500);
                entity.Property(e => e.MeetingPlatform).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("scheduled");
                entity.Property(e => e.ReminderMinutesBefore).HasDefaultValue(15);
                entity.Property(e => e.IsRecurring).HasDefaultValue(false);
                entity.Property(e => e.RecurrencePattern).HasMaxLength(100);
                entity.Property(e => e.CalendarColor).HasMaxLength(7).HasDefaultValue("#2196F3");
                entity.Property(e => e.ExternalEventId).HasMaxLength(255);
                entity.Property(e => e.ExternalSyncStatus).HasMaxLength(20).HasDefaultValue("not_synced");
                entity.Property(e => e.Notes).HasMaxLength(4000);

                // EXPLICITLY MAP BASEENTITY PROPERTIES
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraint for dates
                entity.HasCheckConstraint("CK_Appointments_Dates", "StartDateTime < EndDateTime");

                // Foreign key to User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Appointments)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => new { e.StartDateTime, e.EndDateTime });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.AppointmentType);
                entity.HasIndex(e => new { e.UserID, e.StartDateTime, e.EndDateTime })
                    .HasFilter("[IsDeleted] = 0 AND [Status] != 'cancelled'");
            });

            // ================================================================
            // Participants Table
            // ================================================================
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.ToTable("Participants", "app");
                entity.HasKey(e => e.ParticipantID);
                entity.Property(e => e.ParticipantID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.InvitationStatus).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ParticipantRole).HasMaxLength(20).HasDefaultValue("attendee");

                entity.Ignore(e => e.DeletedAt);
                entity.Ignore(e => e.IsDeleted);
                // EXPLICITLY MAP BASEENTITY PROPERTIES
                //entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                //entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key to Appointment
                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.Participants)
                    .HasForeignKey(e => e.AppointmentID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Foreign key to User (optional)
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.AppointmentID);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.InvitationStatus);
                entity.HasIndex(e => new { e.AppointmentID, e.Email }).IsUnique();
                entity.HasIndex(e => new { e.AppointmentID, e.UserID }).IsUnique();

                // Check constraints
                entity.HasCheckConstraint("CHK_Participant_Status",
                    "[InvitationStatus] IN ('pending', 'sent', 'accepted', 'declined', 'tentative')");
                entity.HasCheckConstraint("CHK_Participant_Role",
                    "[ParticipantRole] IN ('organizer', 'attendee', 'optional')");
            });

            // ================================================================
            // Reminders Table
            // ================================================================
            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.ToTable("Reminders", "app");
                entity.HasKey(e => e.ReminderID);
                entity.Property(e => e.ReminderID).HasDefaultValueSql("NEWID()");

                // Explicitly map properties to database columns
                entity.Property(e => e.UserID).HasColumnName("UserID");
                entity.Property(e => e.TaskID).HasColumnName("TaskID");
                entity.Property(e => e.AppointmentID).HasColumnName("AppointmentID");
                entity.Property(e => e.MeetingID).HasColumnName("MeetingID").IsRequired(false);
                entity.Property(e => e.EventID).HasColumnName("EventID").IsRequired(false);
                entity.Property(e => e.ReminderType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Message).HasMaxLength(500);
                entity.Property(e => e.ReminderDateTime).IsRequired();
                entity.Property(e => e.NotifyViaEmail);
                entity.Property(e => e.NotifyViaPush);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.IsRecurring);
                entity.Property(e => e.SentAt);
                // CreatedAt and UpdatedAt exist in the remote table, so keep them
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");

                // 🔥 CRITICAL: Ignore columns that DO NOT exist in the remote database
                entity.Ignore(e => e.DeletedAt);
                entity.Ignore(e => e.IsDeleted);

                // Foreign key relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Reminders)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Reminders_Users");

                entity.HasOne(e => e.Task)
                    .WithMany(t => t.Reminders)
                    .HasForeignKey(e => e.TaskID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.Reminders)
                    .HasForeignKey(e => e.AppointmentID)
                    .OnDelete(DeleteBehavior.NoAction);

                // NEW: Meeting and Event foreign keys
                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Reminders)
                    .HasForeignKey(e => e.MeetingID)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);

                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.Reminders)
                    .HasForeignKey(e => e.EventID)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.ReminderDateTime);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.Status, e.ReminderDateTime })
                    .HasFilter("[Status] = 'pending'");
            });

            // ================================================================
            // ActivityLogs Table (audit schema)
            // ================================================================
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("ActivityLogs", "audit");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).ValueGeneratedOnAdd();

                entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityType).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("success");
                entity.Property(e => e.Details).HasMaxLength(4000);
                entity.Property(e => e.CreatedAt).IsRequired();

                // Foreign key to User
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.ActivityType);
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.UserID, e.CreatedAt });
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAt });

                // Check constraints
                entity.HasCheckConstraint("CHK_ActivityLog_Status",
                    "[Status] IN ('success', 'error', 'warning')");
                entity.HasCheckConstraint("CHK_ActivityLog_Type",
                    "[ActivityType] IN ('login', 'logout', 'create_task', 'update_task', 'delete_task', " +
                    "'create_appointment', 'update_appointment', 'delete_appointment', 'share_item', " +
                    "'export_data', 'change_settings', 'create_user', 'update_user', 'delete_user')");
            });

            // ═══════════════════════════════════════════════════════════════
            // NEW ENTITY CONFIGURATIONS
            // ═══════════════════════════════════════════════════════════════

            // ================================================================
            // Meetings Table
            // ================================================================
            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.ToTable("Meetings", "app");
                entity.HasKey(e => e.MeetingID);
                entity.Property(e => e.MeetingID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.MeetingLink).HasMaxLength(500);
                entity.Property(e => e.MeetingPlatform).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("scheduled");
                entity.Property(e => e.RecurrencePattern).HasMaxLength(100);

                // BaseEntity properties
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraints
                entity.HasCheckConstraint("CK_Meetings_Dates", "EndDateTime > StartDateTime");
                entity.HasCheckConstraint("CK_Meetings_Status", "[Status] IN ('scheduled','live','ended','cancelled')");

                // Foreign keys
                entity.HasOne(e => e.Task)
                    .WithOne(t => t.Meeting)
                    .HasForeignKey<Meeting>(e => e.TaskID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Organizer)
                    .WithMany(u => u.OrganizedMeetings)
                    .HasForeignKey(e => e.OrganizerUserID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.TaskID).IsUnique();
                entity.HasIndex(e => e.OrganizerUserID);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartDateTime);
            });

            // ================================================================
            // MeetingParticipants Table
            // ================================================================
            modelBuilder.Entity<MeetingParticipant>(entity =>
            {
                entity.ToTable("MeetingParticipants", "app");
                entity.HasKey(e => e.ParticipantID);
                entity.Property(e => e.ParticipantID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.InvitationStatus).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ParticipantRole).HasMaxLength(20).HasDefaultValue("attendee");

                // BaseEntity properties
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraints
                entity.HasCheckConstraint("CHK_MeetingParticipant_Status",
                    "[InvitationStatus] IN ('pending','sent','accepted','declined','tentative')");
                entity.HasCheckConstraint("CHK_MeetingParticipant_Role",
                    "[ParticipantRole] IN ('organizer','attendee','optional')");

                // Foreign keys
                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Participants)
                    .HasForeignKey(e => e.MeetingID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.MeetingParticipations)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.MeetingID);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => new { e.MeetingID, e.Email }).IsUnique();
            });

            // ================================================================
            // EventCategories Table
            // ================================================================
            modelBuilder.Entity<EventCategory>(entity =>
            {
                entity.ToTable("EventCategories", "app");
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.ColorCode).HasMaxLength(7).HasDefaultValue("#7C6CF8");

                // BaseEntity properties
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraint
                entity.HasCheckConstraint("CHK_EventCategories_Color",
                    "ColorCode LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");

                // Foreign key
                entity.HasOne(e => e.User)
                    .WithMany(u => u.CustomEventCategories)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.IsSystem);
            });

            // ================================================================
            // Events Table
            // ================================================================
            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events", "app");
                entity.HasKey(e => e.EventID);
                entity.Property(e => e.EventID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Format).HasMaxLength(500);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("planned");
                entity.Property(e => e.RecurrencePattern).HasMaxLength(100);

                // BaseEntity properties
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraint
                entity.HasCheckConstraint("CHK_Events_Status",
                    "[Status] IN ('planned','ongoing','completed','cancelled')");

                // Foreign keys
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Events)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Events)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.CategoryID);
                entity.HasIndex(e => e.TaskID);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartDateTime);
            });

            // ================================================================
            // EventParticipants Table
            // ================================================================
            modelBuilder.Entity<EventParticipant>(entity =>
            {
                entity.ToTable("EventParticipants", "app");
                entity.HasKey(e => e.ParticipantID);
                entity.Property(e => e.ParticipantID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.InvitationStatus).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ParticipantRole).HasMaxLength(20).HasDefaultValue("attendee");

                // BaseEntity properties
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraints
                entity.HasCheckConstraint("CHK_EventParticipant_Status",
                    "[InvitationStatus] IN ('pending','sent','accepted','declined','tentative')");
                entity.HasCheckConstraint("CHK_EventParticipant_Role",
                    "[ParticipantRole] IN ('organizer','attendee','optional')");

                // Foreign keys
                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.Participants)
                    .HasForeignKey(e => e.EventID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.EventParticipations)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.EventID);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => new { e.EventID, e.Email }).IsUnique();
            });

            // ================================================================
            // UserConnections Table
            // ================================================================
            modelBuilder.Entity<UserConnection>(entity =>
            {
                entity.ToTable("UserConnections", "app");
                entity.HasKey(e => e.ConnectionID);
                entity.Property(e => e.ConnectionID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraint
                entity.HasCheckConstraint("CHK_Connection_Status",
                    "[Status] IN ('pending','accepted','declined','blocked')");

                // Foreign keys
                entity.HasOne(e => e.Requester)
                    .WithMany(u => u.SentConnectionRequests)
                    .HasForeignKey(e => e.RequesterUserID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Recipient)
                    .WithMany(u => u.ReceivedConnectionRequests)
                    .HasForeignKey(e => e.RecipientUserID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Unique constraint
                entity.HasIndex(e => new { e.RequesterUserID, e.RecipientUserID }).IsUnique();

                // Indexes
                entity.HasIndex(e => e.RequesterUserID);
                entity.HasIndex(e => e.RecipientUserID);
            });

            // ================================================================
            // Conversations Table
            // ================================================================
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversations", "app");
                entity.HasKey(e => e.ConversationID);
                entity.Property(e => e.ConversationID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Type).HasMaxLength(10).HasDefaultValue("direct");
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Check constraint
                entity.HasCheckConstraint("CHK_Conversation_Type", "[Type] IN ('direct','group')");
            });

            // ================================================================
            // ConversationParticipants Table
            // ================================================================
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.ToTable("ConversationParticipants", "app");
                entity.HasKey(e => e.ParticipantID);
                entity.Property(e => e.ParticipantID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign keys
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(e => e.ConversationID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ConversationParticipations)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint
                entity.HasIndex(e => new { e.ConversationID, e.UserID }).IsUnique();

                // Indexes
                entity.HasIndex(e => e.ConversationID);
                entity.HasIndex(e => e.UserID);
            });

            // ================================================================
            // Messages Table
            // ================================================================
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages", "app");
                entity.HasKey(e => e.MessageID);
                entity.Property(e => e.MessageID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.SentAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign keys
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.Messages)
                    .HasForeignKey(e => e.SenderUserID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => new { e.ConversationID, e.SentAt });
                entity.HasIndex(e => e.SenderUserID);
            });

            // ================================================================
            // Notifications Table
            // ================================================================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications", "app");
                entity.HasKey(e => e.NotificationID);
                entity.Property(e => e.NotificationID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Message).HasMaxLength(500);
                entity.Property(e => e.EntityType).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => new { e.UserID, e.IsRead, e.CreatedAt });
            });

            // ================================================================
            // Notes Table
            // ================================================================
            modelBuilder.Entity<Note>(entity =>
            {
                entity.ToTable("Notes", "app");
                entity.HasKey(e => e.NoteID);
                entity.Property(e => e.NoteID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).HasMaxLength(255);
                entity.Property(e => e.Content).IsRequired();

                // BaseEntity properties
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign keys
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Notes)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Task)
                    .WithMany(t => t.NotesList)
                    .HasForeignKey(e => e.TaskID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.Notes)
                    .HasForeignKey(e => e.EventID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Appointment)
                    .WithMany()
                    .HasForeignKey(e => e.AppointmentID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Meeting)
                    .WithMany(m => m.Notes)
                    .HasForeignKey(e => e.MeetingID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.TaskID);
                entity.HasIndex(e => e.EventID);
            });

            // ================================================================
            // EventLog Table (audit schema)
            // ================================================================
            modelBuilder.Entity<EventLog>(entity =>
            {
                entity.ToTable("EventLog", "audit");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.LogID).ValueGeneratedOnAdd();
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntitySchema).HasMaxLength(50);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.LogLevel).HasMaxLength(20).HasDefaultValue("Info");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => new { e.EntityName, e.EntityID });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Action);
            });

            // ================================================================
            // DailyStats Table (report schema)
            // ================================================================
            modelBuilder.Entity<DailyStat>(entity =>
            {
                entity.ToTable("DailyStats", "report");
                entity.HasKey(e => e.StatId);
                entity.Property(e => e.StatId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.StatDate).IsRequired();
                entity.Property(e => e.TotalTasks).HasDefaultValue(0);
                entity.Property(e => e.CompletedTasks).HasDefaultValue(0);
                entity.Property(e => e.IncompleteTasks).HasDefaultValue(0);
                entity.Property(e => e.OverdueTasks).HasDefaultValue(0);
                entity.Property(e => e.PersonalTasks).HasDefaultValue(0);
                entity.Property(e => e.JobTasks).HasDefaultValue(0);
                entity.Property(e => e.UnspecifiedTasks).HasDefaultValue(0);
                entity.Property(e => e.AppointmentTasks).HasDefaultValue(0);
                entity.Property(e => e.TotalAppointments).HasDefaultValue(0);
                entity.Property(e => e.CompletedAppointments).HasDefaultValue(0);
                entity.Property(e => e.CancelledAppointments).HasDefaultValue(0);
                entity.Property(e => e.ProductivityScore).HasColumnType("decimal(5,2)");
                entity.Property(e => e.CurrentStreakDays).HasDefaultValue(0);
                entity.Property(e => e.CalculatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // EXPLICITLY MAP BASEENTITY PROPERTIES
                entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
                entity.Property(e => e.DeletedAt).HasColumnName("DeletedAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key to User
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: User + Date
                entity.HasIndex(e => new { e.UserID, e.StatDate }).IsUnique();

                // Indexes
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.StatDate);
                entity.HasIndex(e => e.ProductivityScore);
                entity.HasIndex(e => new { e.UserID, e.StatDate, e.ProductivityScore });
            });
        }
    }
}