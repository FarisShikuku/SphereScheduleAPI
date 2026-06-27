using AutoMapper;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;

namespace SphereScheduleAPI.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ─────────────────────────────────────────────────────────────────────
            // User Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<User, UserDto>();

            CreateMap<UpdateUserDto, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateUserProfileDto, UpdateUserDto>();

            // ─────────────────────────────────────────────────────────────────────
            // Task Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<TaskEntity, TaskDto>()
                .ForMember(dest => dest.SubtaskCount,
                    opt => opt.MapFrom(src => src.Subtasks.Count))
                .ForMember(dest => dest.CompletedSubtasks,
                    opt => opt.MapFrom(src => src.Subtasks.Count(s => s.Status == "completed")))
                .ForMember(dest => dest.ReminderCount,
                    opt => opt.MapFrom(src => src.Reminders.Count))
                .ForMember(dest => dest.DueStatus,
                    opt => opt.MapFrom(src => CalculateDueStatus(src.DueDate, src.Status)))
                .ForMember(dest => dest.DaysUntilDue,
                    opt => opt.MapFrom(src => CalculateDaysUntilDue(src.DueDate)));

            CreateMap<CreateTaskDto, TaskEntity>()
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
                .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.LocationName ?? string.Empty))
                .ForMember(dest => dest.LocationAddress, opt => opt.MapFrom(src => src.LocationAddress ?? string.Empty))
                .ForMember(dest => dest.RecurrenceRule, opt => opt.MapFrom(src => src.RecurrenceRule ?? string.Empty))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags ?? string.Empty))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes ?? string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "pending"))
                .ForMember(dest => dest.CompletionPercentage, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.ExternalSyncStatus, opt => opt.MapFrom(_ => "not_synced"));

            CreateMap<UpdateTaskDto, TaskEntity>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ─────────────────────────────────────────────────────────────────────
            // Appointment Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.ParticipantCount,
                    opt => opt.MapFrom(src => src.Participants.Count))
                .ForMember(dest => dest.ReminderCount,
                    opt => opt.MapFrom(src => src.Reminders.Count));

            CreateMap<CreateAppointmentDto, Appointment>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "scheduled"))
                .ForMember(dest => dest.ExternalSyncStatus, opt => opt.MapFrom(_ => "not_synced"));

            CreateMap<UpdateAppointmentDto, Appointment>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ─────────────────────────────────────────────────────────────────────
            // Subtask Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<Subtask, SubtaskDto>();

            CreateMap<CreateSubtaskDto, Subtask>()
                .ForMember(dest => dest.SubTaskID, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false));

            CreateMap<UpdateSubtaskDto, Subtask>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ─────────────────────────────────────────────────────────────────────
            // Category Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.TaskCount, opt => opt.Ignore())
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.Ignore());

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false));

            CreateMap<UpdateCategoryDto, Category>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ─────────────────────────────────────────────────────────────────────
            // Reminder Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<Reminder, ReminderDto>();

            CreateMap<CreateReminderDto, Reminder>()
                .ForMember(dest => dest.ReminderID, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "pending"));

            CreateMap<UpdateReminderDto, Reminder>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ─────────────────────────────────────────────────────────────────────
            // Participant Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<Participant, ParticipantDto>();

            CreateMap<CreateParticipantDto, Participant>()
                .ForMember(dest => dest.ParticipantID, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.InvitationStatus, opt => opt.MapFrom(src => src.InvitationStatus ?? "pending"))
                .ForMember(dest => dest.ParticipantRole, opt => opt.MapFrom(src => src.ParticipantRole ?? "attendee"));

            CreateMap<UpdateParticipantDto, Participant>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ─────────────────────────────────────────────────────────────────────
            // ActivityLog Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<ActivityLog, ActivityLogDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.Ignore())
                .ForMember(dest => dest.UserDisplayName, opt => opt.Ignore())
                .ForMember(dest => dest.EntityTitle, opt => opt.Ignore());

            CreateMap<CreateActivityLogDto, ActivityLog>()
                .ForMember(dest => dest.LogId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // ─────────────────────────────────────────────────────────────────────
            // DailyStat Mappings
            // ─────────────────────────────────────────────────────────────────────
            CreateMap<DailyStat, DailyStatDto>();


            CreateMap<Meeting, MeetingDto>()
                .ForMember(dest => dest.OrganizerName, opt => opt.MapFrom(src => src.Organizer.DisplayName))
                .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => src.Task.Title))
                .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count));

            CreateMap<MeetingParticipant, MeetingParticipantDto>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : null))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

            CreateMap<CreateMeetingDto, Meeting>()
                .ForMember(dest => dest.MeetingID, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizerUserID, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "scheduled"))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Organizer, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Reminders, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore());

            CreateMap<CreateMeetingParticipantDto, MeetingParticipant>()
                .ForMember(dest => dest.ParticipantID, opt => opt.Ignore())
                .ForMember(dest => dest.MeetingID, opt => opt.Ignore())
                .ForMember(dest => dest.InvitationStatus, opt => opt.MapFrom(_ => "pending"))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Meeting, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // ═══════════════════════════════════════════
            // NEW: Event Mappings
            // ═══════════════════════════════════════════
            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : null))
                .ForMember(dest => dest.CategoryColor, opt => opt.MapFrom(src => src.Category != null ? src.Category.ColorCode : null))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count))
                .ForMember(dest => dest.ReminderCount, opt => opt.MapFrom(src => src.Reminders.Count));

            CreateMap<EventParticipant, EventParticipantDto>();

            CreateMap<EventCategory, EventCategoryDto>()
                .ForMember(dest => dest.EventCount, opt => opt.MapFrom(src => src.Events.Count));

            CreateMap<CreateEventDto, Event>()
                .ForMember(dest => dest.EventID, opt => opt.Ignore())
                .ForMember(dest => dest.UserID, opt => opt.Ignore())
                .ForMember(dest => dest.TaskID, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "planned"))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Reminders, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore());

            CreateMap<CreateEventParticipantDto, EventParticipant>()
                .ForMember(dest => dest.ParticipantID, opt => opt.Ignore())
                .ForMember(dest => dest.EventID, opt => opt.Ignore())
                .ForMember(dest => dest.InvitationStatus, opt => opt.MapFrom(_ => "pending"))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<CreateEventCategoryDto, EventCategory>()
                .ForMember(dest => dest.CategoryID, opt => opt.Ignore())
                .ForMember(dest => dest.UserID, opt => opt.Ignore())
                .ForMember(dest => dest.IsSystem, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Events, opt => opt.Ignore());

            // ═══════════════════════════════════════════
            // NEW: Chat Mappings
            // ═══════════════════════════════════════════
            CreateMap<UserConnection, UserConnectionDto>()
                .ForMember(dest => dest.RequesterDisplayName, opt => opt.MapFrom(src => src.Requester.DisplayName))
                .ForMember(dest => dest.RequesterAvatarUrl, opt => opt.MapFrom(src => src.Requester.AvatarUrl))
                .ForMember(dest => dest.RecipientDisplayName, opt => opt.MapFrom(src => src.Recipient.DisplayName))
                .ForMember(dest => dest.RecipientAvatarUrl, opt => opt.MapFrom(src => src.Recipient.AvatarUrl));

            CreateMap<Conversation, ConversationDto>();

            CreateMap<ConversationParticipant, ConversationParticipantDto>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl));

            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderDisplayName, opt => opt.MapFrom(src => src.Sender.DisplayName))
                .ForMember(dest => dest.SenderAvatarUrl, opt => opt.MapFrom(src => src.Sender.AvatarUrl));

            // ═══════════════════════════════════════════
            // NEW: Note Mappings
            // ═══════════════════════════════════════════
            CreateMap<Note, NoteDto>()
                .ForMember(dest => dest.LinkedEntityTitle, opt => opt.Ignore()); // Set manually in service

            CreateMap<CreateNoteDto, Note>()
                .ForMember(dest => dest.NoteID, opt => opt.Ignore())
                .ForMember(dest => dest.UserID, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.Meeting, opt => opt.Ignore());

            // ═══════════════════════════════════════════
            // NEW: Notification Mappings
            // ═══════════════════════════════════════════
            CreateMap<Notification, NotificationDto>();

            // ═══════════════════════════════════════════
            // NEW: EventLog Mappings
            // ═══════════════════════════════════════════
            CreateMap<EventLog, EventLogDto>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : null))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helper Methods
        // ─────────────────────────────────────────────────────────────────────────
        private static string CalculateDueStatus(DateTime? dueDate, string status)
        {
            if (status == "completed" || status == "cancelled")
                return status;

            if (!dueDate.HasValue)
                return "no_due_date";

            var today = DateTime.Today;
            var daysDiff = (dueDate.Value.Date - today).Days;

            if (daysDiff < 0)
                return "overdue";
            if (daysDiff == 0)
                return "today";
            if (daysDiff == 1)
                return "tomorrow";
            if (daysDiff <= 7)
                return "this_week";

            return "future";
        }

        private static int? CalculateDaysUntilDue(DateTime? dueDate)
        {
            if (!dueDate.HasValue)
                return null;

            var today = DateTime.Today;
            return (dueDate.Value.Date - today).Days;
        }
    }
}