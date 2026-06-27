using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Infrastructure.Services;

namespace SphereScheduleAPI.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddDatabase(configuration)
                .AddInfrastructureServices();

            return services;
        }

        // ─── Database ─────────────────────────────────────────────────────────
        private static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

            return services;
        }

        // ─── Infrastructure Services ──────────────────────────────────────────
        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Scoped so they can safely consume scoped dependencies in future
            services.AddScoped<JwtService>();
            services.AddScoped<PasswordService>();

            return services;
        }
    }
}