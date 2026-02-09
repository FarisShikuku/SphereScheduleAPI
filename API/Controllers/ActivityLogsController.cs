using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")] // Only admins can access activity logs
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<ActivityLogsController> _logger;

        public ActivityLogsController(
            IActivityLogService activityLogService,
            ILogger<ActivityLogsController> logger)
        {
            _activityLogService = activityLogService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), 200)]
        public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogFilterDto filterDto)
        {
            var activityLogs = await _activityLogService.GetActivityLogsByFilterAsync(filterDto);
            return Ok(activityLogs);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ActivityLogDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetActivityLogById(long id, [FromQuery] bool includeDetails = false)
        {
            var activityLog = await _activityLogService.GetActivityLogByIdAsync(id, includeDetails);
            return Ok(activityLog);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ActivityLogDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateActivityLog([FromBody] CreateActivityLogDto createDto)
        {
            var activityLog = await _activityLogService.CreateActivityLogAsync(createDto);
            return CreatedAtAction(nameof(GetActivityLogById), new { id = activityLog.LogId }, activityLog);
        }

        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserActivityLogs(Guid userId, [FromQuery] ActivityLogFilterDto filterDto)
        {
            var activityLogs = await _activityLogService.GetActivityLogsByUserIdAsync(userId, filterDto);
            return Ok(activityLogs);
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), 200)]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int count = 20)
        {
            var activities = await _activityLogService.GetRecentActivitiesAsync(count);
            return Ok(activities);
        }

        [HttpGet("stats")]
        [ProducesResponseType(typeof(ActivityStatisticsDto), 200)]
        public async Task<IActionResult> GetActivityStatistics(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var statistics = await _activityLogService.GetActivityStatisticsAsync(startDate, endDate);
            return Ok(statistics);
        }

        [HttpGet("user/{userId}/summary")]
        [ProducesResponseType(typeof(UserActivitySummaryDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserActivitySummary(Guid userId)
        {
            var summary = await _activityLogService.GetUserActivitySummaryAsync(userId);
            return Ok(summary);
        }

        [HttpGet("audit/{entityType}/{entityId}")]
        [ProducesResponseType(typeof(AuditTrailDto), 200)]
        public async Task<IActionResult> GetAuditTrail(string entityType, Guid entityId)
        {
            var auditTrail = await _activityLogService.GetAuditTrailAsync(entityType, entityId);
            return Ok(auditTrail);
        }

        [HttpGet("failed")]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), 200)]
        public async Task<IActionResult> GetFailedActivities([FromQuery] DateTimeOffset? since = null)
        {
            var failedActivities = await _activityLogService.GetFailedActivitiesAsync(since);
            return Ok(failedActivities);
        }

        [HttpPost("cleanup")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
        {
            var cleanedCount = await _activityLogService.CleanupOldLogsAsync(daysToKeep);
            return Ok(new
            {
                message = $"Cleaned up {cleanedCount} old activity logs",
                cleanedCount = cleanedCount,
                daysToKeep = daysToKeep
            });
        }

        [HttpGet("my-activity")]
        [AllowAnonymous] // Allow users to see their own activity
        [ProducesResponseType(typeof(IEnumerable<ActivityLogDto>), 200)]
        public async Task<IActionResult> GetMyActivity([FromQuery] ActivityLogFilterDto filterDto)
        {
            var userId = GetUserIdFromToken();
            filterDto.UserId = userId;

            var activityLogs = await _activityLogService.GetActivityLogsByFilterAsync(filterDto);
            return Ok(activityLogs);
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var statistics = await _activityLogService.GetActivityStatisticsAsync(
                DateTimeOffset.UtcNow.AddDays(-30),
                DateTimeOffset.UtcNow
            );

            var recentActivities = await _activityLogService.GetRecentActivitiesAsync(10);
            var failedActivities = await _activityLogService.GetFailedActivitiesAsync(DateTimeOffset.UtcNow.AddDays(-1));

            return Ok(new
            {
                Statistics = statistics,
                RecentActivities = recentActivities,
                RecentFailedActivities = failedActivities,
                TotalUsers = await GetTotalUserCountAsync(),
                ActiveUsersToday = await GetActiveUsersTodayCountAsync()
            });
        }

        private Guid GetUserIdFromToken()
        {
            // Extract user ID from JWT token
            // For demo, returning a hardcoded ID
            return Guid.Parse("12345678-1234-1234-1234-123456789abc");
        }

        private async Task<int> GetTotalUserCountAsync()
        {
            // This would come from your user service
            return 0;
        }

        private async Task<int> GetActiveUsersTodayCountAsync()
        {
            // Count users with activity today
            var today = DateTimeOffset.UtcNow.Date;
            return 0;
        }
    }
}