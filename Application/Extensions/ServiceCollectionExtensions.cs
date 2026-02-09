using Microsoft.Extensions.DependencyInjection;
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

            // Fix: Use proper AutoMapper registration
            services.AddAutoMapper(config =>
            {
                config.AddProfile<MappingProfile>();
                config.AddProfile<ReminderProfile>();
                config.AddProfile<ParticipantProfile>();
            });

            // Alternative: If you have multiple assemblies with profiles
            // services.AddAutoMapper(typeof(MappingProfile).Assembly);

            return services;
        }
    }
}