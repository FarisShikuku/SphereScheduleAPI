using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            HealthCheckService healthCheckService,
            ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetHealth()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var status = report.Status switch
            {
                HealthStatus.Healthy => "healthy",
                HealthStatus.Degraded => "degraded",
                _ => "unhealthy"
            };

            var response = new
            {
                status,
                timestamp = DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLower(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    exception = e.Value.Exception?.Message,
                    data = e.Value.Data
                })
            };

            _logger.LogInformation("Health check executed with status {Status}", status);

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(503, response);
        }

        [HttpGet("live")]
        public ActionResult GetLiveness()
        {
            _logger.LogInformation("Liveness check executed");
            return Ok(new
            {
                status = "live",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("ready")]
        public async Task<ActionResult> GetReadiness()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var status = report.Status == HealthStatus.Healthy ? "ready" : "not ready";
            _logger.LogInformation("Readiness check executed with status {Status}", status);

            return report.Status == HealthStatus.Healthy
                ? Ok(new { status, timestamp = DateTime.UtcNow })
                : StatusCode(503, new { status, timestamp = DateTime.UtcNow });
        }

        [HttpGet("database")]
        public async Task<ActionResult> GetDatabaseHealth()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            if (!report.Entries.TryGetValue("DbContext", out var dbCheck))
            {
                _logger.LogWarning("Database health check not configured");
                return StatusCode(503, new { status = "database check not configured" });
            }

            if (dbCheck.Status == HealthStatus.Healthy)
            {
                _logger.LogInformation("Database connected successfully");
                return Ok(new { status = "database connected", timestamp = DateTime.UtcNow });
            }
            else
            {
                _logger.LogError(dbCheck.Exception, "Database connection failed");
                return StatusCode(503, new
                {
                    status = "database connection failed",
                    error = dbCheck.Exception?.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
