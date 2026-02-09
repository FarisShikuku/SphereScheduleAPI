using AutoMapper;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;

namespace SphereScheduleAPI.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {

            
          
            // User mappings
            CreateMap<User, UserDto>();
               //.ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
               //.ForMember(dest => dest.PasswordSalt, opt => opt.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // UpdateUserProfileDto to UpdateUserDto mapping
            CreateMap<UpdateUserProfileDto, UpdateUserDto>();

            // Task mappings
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
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(_ => "pending"))
                .ForMember(dest => dest.CompletionPercentage,
                    opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.ExternalSyncStatus,
                    opt => opt.MapFrom(_ => "not_synced"));

            CreateMap<UpdateTaskDto, TaskEntity>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));



            // Appointment mappings
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.ParticipantCount,
                    opt => opt.MapFrom(src => src.Participants.Count))
                .ForMember(dest => dest.ReminderCount,
                    opt => opt.MapFrom(src => src.Reminders.Count));

            CreateMap<CreateAppointmentDto, Appointment>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(_ => "scheduled"))
                .ForMember(dest => dest.ExternalSyncStatus,
                    opt => opt.MapFrom(_ => "not_synced"));

            CreateMap<UpdateAppointmentDto, Appointment>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Subtask mappings
            CreateMap<Subtask, SubtaskDto>();

            // In the MappingProfile constructor, add these mappings:

            // Category mappings
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.TaskCount, opt => opt.Ignore())
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.Ignore());

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false));

            CreateMap<UpdateCategoryDto, Category>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Reminder mappings
            CreateMap<Domain.Entities.Reminder, DTOs.ReminderDto>();
            CreateMap<DTOs.CreateReminderDto, Domain.Entities.Reminder>();
            CreateMap<DTOs.UpdateReminderDto, Domain.Entities.Reminder>();
            // DailyStat mappings
            CreateMap<DailyStat, DailyStatDto>();
            // Subtask mappings
            CreateMap<Subtask, SubtaskDto>();
            CreateMap<CreateSubtaskDto, Subtask>()
                .ForMember(dest => dest.SubtaskId, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false));

            CreateMap<UpdateSubtaskDto, Subtask>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ActivityLog Mappings
            CreateMap<ActivityLog, ActivityLogDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.Ignore())
                .ForMember(dest => dest.UserDisplayName, opt => opt.Ignore())
                .ForMember(dest => dest.EntityTitle, opt => opt.Ignore());

            CreateMap<CreateActivityLogDto, ActivityLog>()
                .ForMember(dest => dest.LogId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());
        }

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