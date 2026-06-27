using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SphereScheduleAPI.API.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

namespace SphereScheduleAPI.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddJwtAuthentication(configuration)
                .AddCorsPolicy()
                .AddSwaggerDocumentation()
                .AddHealthChecks(configuration);

            return services;
        }

        // ─── JWT Authentication ───────────────────────────────────────────────
        private static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            var secretKey = jwtSection["SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

            var key = Encoding.UTF8.GetBytes(secretKey);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection["Issuer"] ?? "SphereScheduleAPI",
                        ValidateAudience = true,
                        ValidAudience = jwtSection["Audience"] ?? "SphereScheduleClient",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception is SecurityTokenExpiredException)
                                context.Response.Headers.Append("Token-Expired", "true");

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }

        // ─── CORS ─────────────────────────────────────────────────────────────
        private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            return services;
        }

        // ─── Swagger / OpenAPI ────────────────────────────────────────────────
        private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Sphere Schedule API",
                    Version = "v1",
                    Description = "API for Sphere Schedule – A comprehensive task and appointment management system.",
                    Contact = new OpenApiContact
                    {
                        Name = "Sphere Schedule Team",
                        Email = "support@sphereschedule.com"
                    }
                });

                // JWT security definition
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.\r\nExample: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", doc),
                        new List<string>()
                    }
                });

                c.OperationFilter<AuthorizationOperationFilter>();
                c.EnableAnnotations();
            });

            return services;
        }

        // ─── Health Checks ────────────────────────────────────────────────────
        private static IServiceCollection AddHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHealthChecks()
                    .AddDbContextCheck<Infrastructure.Data.ApplicationDbContext>();

            return services;
        }
    }
}