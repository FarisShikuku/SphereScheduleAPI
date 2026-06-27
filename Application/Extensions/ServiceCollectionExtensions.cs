using AutoMapper;

using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.Mappings;
using SphereScheduleAPI.Application.Services;

namespace SphereScheduleAPI.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register application services with interfaces
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IUserService, UserManagementService>();
            services.AddScoped<ITaskService, TaskManagementService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IReminderService, ReminderService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDailyStatService, DailyStatService>();
            services.AddScoped<ISubtaskService, SubtaskService>();
            services.AddScoped<IParticipantService, ParticipantService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();
            // Application/Extensions/ServiceCollectionExtensions.cs
            // Add these lines inside your existing AddApplicationServices method

            // ═══════════════════════════════════════════
            // NEW: Meeting Services
            // ═══════════════════════════════════════════
            services.AddScoped<IMeetingService, MeetingService>();

            // ═══════════════════════════════════════════
            // NEW: Event Services
            // ═══════════════════════════════════════════
            services.AddScoped<IEventService, EventService>();

            // ═══════════════════════════════════════════
            // NEW: Chat Services
            // ═══════════════════════════════════════════
            services.AddScoped<IChatService, ChatService>();

            // ═══════════════════════════════════════════
            // NEW: Note Services
            // ═══════════════════════════════════════════
            services.AddScoped<INoteService, NoteService>();

            // ═══════════════════════════════════════════
            // NEW: Notification Services
            // ═══════════════════════════════════════════
            services.AddScoped<INotificationService, NotificationService>();

            // ═══════════════════════════════════════════
            // NEW: EventLog Services
            // ═══════════════════════════════════════════
            services.AddScoped<IEventLogService, EventLogService>();

            // Register AutoMapper profiles via configuration action to match available overloads.
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
                
            });

            return services;
        }
    }
}