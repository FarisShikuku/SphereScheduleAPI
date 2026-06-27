using SphereScheduleAPI.API.Extensions;
using SphereScheduleAPI.API.Middlewares;
using SphereScheduleAPI.Application.Extensions;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ─── Register Services ────────────────────────────────────────────────────────
builder.Services
    .AddInfrastructure(builder.Configuration)   // DB, JwtService, PasswordService
    .AddApplication()                           // AutoMapper, business services
    .AddApiServices(builder.Configuration)      // JWT auth, CORS, Swagger, health
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
});

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));

// ─── Build & Configure Pipeline ───────────────────────────────────────────────
var app = builder.Build();

app.ConfigurePipeline();

Console.WriteLine("=============================================");
Console.WriteLine("Sphere Schedule API v1.0.0");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("Database: Connected to db35194");
Console.WriteLine("Swagger: /api-docs");
Console.WriteLine("Health Check: /health");
Console.WriteLine("=============================================");

app.Run();

// ─── App Settings ─────────────────────────────────────────────────────────────
public class AppSettings
{
    public string Name { get; set; } = "Sphere Schedule";
    public string Version { get; set; } = "1.0.0";
    public string BaseUrl { get; set; } = "https://localhost:5001";
    public bool EnableSwagger { get; set; } = true;
    public bool EnableCors { get; set; } = true;
}