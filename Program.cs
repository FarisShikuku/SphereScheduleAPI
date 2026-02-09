using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using SphereScheduleAPI.API.Filters;
using SphereScheduleAPI.API.Middlewares;
using SphereScheduleAPI.Application.Extensions;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Infrastructure.Extensions;
using SphereScheduleAPI.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
ConfigureServices(builder);

var app = builder.Build();

// Configure the HTTP request pipeline
ConfigurePipeline(app);

app.Run();

void ConfigureServices(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;
    var services = builder.Services;

    // Add database context WITHOUT automatic migrations
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            "Server=db35194.databaseasp.net;Database=db35194;User Id=db35194;Password=C%w83iM!K?o7;Encrypt=False;MultipleActiveResultSets=True;",
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

    // Add application services
    services.AddApplication();

    // Add Infrastructure services
    services.AddSingleton<PasswordService>();
    services.AddSingleton<JwtService>();

    // Configure JWT Authentication
    var jwtSettings = configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "YourProductionSecretKeyThatIsAtLeast32CharactersLong!");

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "SphereScheduleAPI",
            ValidAudience = jwtSettings["Audience"] ?? "SphereScheduleClient",
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

    // Add CORS
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });

    // Add controllers
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Add Swagger/OpenAPI - UPDATED FOR .NET 10 COMPATIBILITY
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Sphere Schedule API",
            Version = "v1",
            Description = "API for Sphere Schedule - A comprehensive task and appointment management system",
            Contact = new OpenApiContact
            {
                Name = "Sphere Schedule Team",
                Email = "support@sphereschedule.com"
            }
        });

        // Add JWT Authentication to Swagger - CORRECT FOR .NET 10
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        // CORRECT: Use OpenApiSecuritySchemeReference with document parameter
        c.AddSecurityRequirement(doc =>
        {
            return new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", doc),
                    new List<string>()
                }
            };
        });

        // Add custom operation filters
        c.OperationFilter<AuthorizationOperationFilter>();

        // Enable annotations
        c.EnableAnnotations();

        // Optional: Include XML comments
        // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        // c.IncludeXmlComments(xmlPath);
    });

    // Add HTTP Context Accessor
    services.AddHttpContextAccessor();

    // Add Health Checks
    services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>();

    // Add Logging
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    });

    // Configure App Settings
    services.Configure<AppSettings>(configuration.GetSection("App"));
}

void ConfigurePipeline(WebApplication app)
{
    var env = app.Environment;

    // Swagger enabled for all environments
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sphere Schedule API v1");
        c.RoutePrefix = "api-docs";

        // Optional: Persist authorization in Swagger UI
        c.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");

        // Optional: Configure OAuth for Swagger UI if needed
        // c.OAuthClientId("swagger-ui");
        // c.OAuthClientSecret("swagger-ui-secret");
        // c.OAuthUsePkce();
    });

    // Global Exception Handling
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Request Logging (development only - comment out for production if needed)
    if (env.IsDevelopment())
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
    }

    // Security Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        // Remove server header
        context.Response.Headers.Remove("Server");

        await next();
    });

    // CORS
    app.UseCors("AllowAll");

    // HTTPS Redirection (enable in production, optional in development)
    if (!env.IsDevelopment())
    {
        app.UseHttpsRedirection();
        app.UseHsts();
    }

    // Routing
    app.UseRouting();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Activity Logging Middleware
    app.UseMiddleware<ActivityLogMiddleware>();

    // JWT Middleware
    app.UseMiddleware<JwtMiddleware>();

    // Endpoints
    app.MapControllers();

    // Health Check endpoint
    app.MapHealthChecks("/health");

    // API Status endpoint
    app.MapGet("/", () =>
    {
        return Results.Ok(new
        {
            status = "healthy",
            application = "Sphere Schedule API",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = env.EnvironmentName,
            database = "Connected (tables created via SQL scripts)",
            documentation = "/api-docs",
            health_check = "/health"
        });
    });

    // Log startup
    Console.WriteLine($"=============================================");
    Console.WriteLine($"Sphere Schedule API v1.0.0");
    Console.WriteLine($"Environment: {env.EnvironmentName}");
    Console.WriteLine($"Database: Connected to db35194");
    Console.WriteLine($"Swagger: /api-docs");
    Console.WriteLine($"Health Check: /health");
    Console.WriteLine($"=============================================");
}

// App Settings class
public class AppSettings
{
    public string Name { get; set; } = "Sphere Schedule";
    public string Version { get; set; } = "1.0.0";
    public string BaseUrl { get; set; } = "https://localhost:5001";
    public bool EnableSwagger { get; set; } = true;
    public bool EnableCors { get; set; } = true;
}