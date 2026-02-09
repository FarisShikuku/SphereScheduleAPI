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

        // Tables in audit schema
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // Tables in report schema
        public DbSet<DailyStat> DailyStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure schema
            modelBuilder.HasDefaultSchema("app");

            // Configure Users table
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", "app");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasMaxLength(512);
                entity.Property(e => e.PasswordSalt).HasMaxLength(512);
                entity.Property(e => e.DisplayName).HasMaxLength(100);
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.AccountType).HasMaxLength(20).HasDefaultValue("free");
                entity.Property(e => e.GoogleId).HasMaxLength(255);
                entity.Property(e => e.MicrosoftId).HasMaxLength(255);
                entity.Property(e => e.FacebookId).HasMaxLength(255);
                entity.Property(e => e.Preferences).HasColumnType("NVARCHAR(MAX)");

                // Default values
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.EmailVerified).HasDefaultValue(false);
                entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
                entity.Property(e => e.LockoutEnabled).HasDefaultValue(false);
                entity.Property(e => e.AccessFailedCount).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

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

            // Configure Tasks table


            // Configure TaskEntity (renamed from Task)
            modelBuilder.Entity<TaskEntity>(entity =>
            {
                entity.ToTable("Tasks", "app");
                entity.HasKey(e => e.TaskId);
                entity.Property(e => e.TaskId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Category).HasMaxLength(20).HasDefaultValue("unspecified");
                entity.Property(e => e.TaskType).HasMaxLength(30).HasDefaultValue("general");
                entity.Property(e => e.PriorityLevel).HasMaxLength(10).HasDefaultValue("medium");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.CompletionPercentage).HasDefaultValue(0);
                entity.Property(e => e.LocationName).HasMaxLength(255);
                entity.Property(e => e.LocationAddress).HasMaxLength(500);
                entity.Property(e => e.Tags).HasMaxLength(500);
                entity.Property(e => e.ExternalSyncStatus).HasMaxLength(20).HasDefaultValue("not_synced");
                entity.Property(e => e.TimeSpentMinutes).HasDefaultValue(0);

                // Check constraint for completion percentage
                entity.HasCheckConstraint("CK_Tasks_CompletionPercentage", "CompletionPercentage BETWEEN 0 AND 100");

                // Foreign keys
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Tasks)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ParentTask)
                    .WithMany(t => t.ChildTasks)
                    .HasForeignKey(e => e.ParentTaskId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.PriorityLevel);
                entity.HasIndex(e => new { e.UserId, e.DueDate, e.Status })
                    .HasFilter("[IsDeleted] = 0");
                entity.HasIndex(e => new { e.UserId, e.Category, e.Status })
                    .HasFilter("[IsDeleted] = 0");
            });

            // Configure Subtask entity
            modelBuilder.Entity<Subtask>(entity =>
            {
                entity.ToTable("Subtasks", "app");
                entity.HasKey(e => e.SubtaskId);
                entity.Property(e => e.SubtaskId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.Priority).HasMaxLength(10).HasDefaultValue("medium");

                // Foreign key
                entity.HasOne(e => e.Task)
                    .WithMany(t => t.Subtasks)
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex(e => e.Status);
            });

            // Configure other entities similarly...

            // Configure audit schema entities
            // Configure ActivityLog entity
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).ValueGeneratedOnAdd();

                entity.Property(e => e.ActivityType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.EntityType)
                    .HasMaxLength(50);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("success");

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for common queries
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ActivityType);
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAt });

                // Check constraint for valid status
                entity.HasCheckConstraint("CHK_ActivityLog_Status",
                    "[Status] IN ('success', 'error', 'warning')");

                // Check constraint for valid activity types (common ones)
                entity.HasCheckConstraint("CHK_ActivityLog_Type",
                    "[ActivityType] IN ('login', 'logout', 'create_task', 'update_task', 'delete_task', " +
                    "'create_appointment', 'update_appointment', 'delete_appointment', 'share_item', " +
                    "'export_data', 'change_settings', 'create_user', 'update_user', 'delete_user')");
            });

            // Add to your OnModelCreating method:

            // Configure DailyStat entity
            modelBuilder.Entity<DailyStat>(entity =>
            {
                entity.ToTable("DailyStats", "report");
                entity.HasKey(e => e.StatId);
                entity.Property(e => e.StatId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.StatDate).IsRequired();
                entity.Property(e => e.ProductivityScore).HasColumnType("decimal(5,2)");
                entity.Property(e => e.CalculatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: User + Date
                entity.HasIndex(e => new { e.UserId, e.StatDate }).IsUnique();

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StatDate);
                entity.HasIndex(e => e.ProductivityScore);
                entity.HasIndex(e => new { e.UserId, e.StatDate, e.ProductivityScore });
            });

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments", "app");
                entity.HasKey(e => e.AppointmentId);
                entity.Property(e => e.AppointmentId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AppointmentType).HasMaxLength(30).HasDefaultValue("general");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("scheduled");
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.MeetingLink).HasMaxLength(500);
                entity.Property(e => e.MeetingPlatform).HasMaxLength(50);
                entity.Property(e => e.RecurrencePattern).HasMaxLength(100);
                entity.Property(e => e.CalendarColor).HasMaxLength(7).HasDefaultValue("#2196F3");
                entity.Property(e => e.ExternalSyncStatus).HasMaxLength(20).HasDefaultValue("not_synced");
                entity.Property(e => e.ReminderMinutesBefore).HasDefaultValue(15);

                // Check constraint for dates
                entity.HasCheckConstraint("CK_Appointments_Dates", "StartDateTime < EndDateTime");

                // Foreign key
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Appointments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.StartDateTime, e.EndDateTime });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.UserId, e.StartDateTime, e.EndDateTime })
                    .HasFilter("[IsDeleted] = 0 AND [Status] != 'cancelled'");
            });

            // Configure Participant entity if needed
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.ToTable("Participants", "app");
                entity.HasKey(e => e.ParticipantId);
                entity.Property(e => e.ParticipantId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.InvitationStatus).HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ParticipantRole).HasMaxLength(20).HasDefaultValue("attendee");

                // Foreign key
                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.Participants)
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.AppointmentId);
                entity.HasIndex(e => e.Email);
            });
            // Add to your OnModelCreating method:

            // Configure Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories", "app");
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CategoryType).HasMaxLength(20).HasDefaultValue("custom");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ColorCode).HasMaxLength(7).HasDefaultValue("#4CAF50");
                entity.Property(e => e.IconName).HasMaxLength(50);
                entity.Property(e => e.CategoryOrder).HasDefaultValue(0);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Foreign key
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Categories)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: User + CategoryName (case-insensitive comparison in application logic)
                entity.HasIndex(e => new { e.UserId, e.CategoryName })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CategoryType);
                entity.HasIndex(e => e.IsDefault);
                entity.HasIndex(e => e.CategoryOrder);
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasKey(e => e.ReminderId);
                entity.Property(e => e.ReminderId).HasDefaultValueSql("NEWID()");

                entity.Property(e => e.ReminderType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("general");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Message)
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(e => e.NotifyViaEmail)
                    .HasDefaultValue(true);

                entity.Property(e => e.NotifyViaPush)
                    .HasDefaultValue(true);

                entity.Property(e => e.IsRecurring)
                    .HasDefaultValue(false);

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Appointment)
                    .WithMany()
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ReminderDateTime);
                entity.HasIndex(e => new { e.Status, e.ReminderDateTime })
                    .HasFilter("[Status] = 'pending'");
            });
            // Configure Participant entity
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.HasKey(e => e.ParticipantId);
                entity.Property(e => e.ParticipantId).HasDefaultValueSql("NEWID()");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FullName)
                    .HasMaxLength(100);

                entity.Property(e => e.InvitationStatus)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(e => e.ParticipantRole)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("attendee");

                // Relationships
                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.Participants)
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes
                entity.HasIndex(e => e.AppointmentId);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.InvitationStatus);
                entity.HasIndex(e => new { e.AppointmentId, e.Email }).IsUnique();
                entity.HasIndex(e => new { e.AppointmentId, e.UserId }).IsUnique();

                // Check constraint for valid status
                entity.HasCheckConstraint("CHK_Participant_Status",
                    "[InvitationStatus] IN ('pending', 'sent', 'accepted', 'declined', 'tentative')");

                entity.HasCheckConstraint("CHK_Participant_Role",
                    "[ParticipantRole] IN ('organizer', 'attendee', 'optional')");
            });

        }
    }
}