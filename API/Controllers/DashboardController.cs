using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using System.Security.Claims;  // For ClaimTypes
using Microsoft.Extensions.Logging;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("overview")]
        [ProducesResponseType(typeof(DashboardOverviewDto), 200)]
        public async Task<IActionResult> GetDashboardOverview([FromQuery] DashboardFilterDto filterDto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var overview = await _dashboardService.GetDashboardOverviewAsync(userId, filterDto);
                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("tasks/stats")]
        [ProducesResponseType(typeof(TaskStatsDto), 200)]
        public async Task<IActionResult> GetTaskStatistics(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var stats = await _dashboardService.GetTaskStatisticsAsync(userId, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("appointments/stats")]
        [ProducesResponseType(typeof(AppointmentStatsDto), 200)]
        public async Task<IActionResult> GetAppointmentStatistics(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var stats = await _dashboardService.GetAppointmentStatisticsAsync(userId, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment statistics");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("productivity/stats")]
        [ProducesResponseType(typeof(ProductivityStatsDto), 200)]
        public async Task<IActionResult> GetProductivityStatistics(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var stats = await _dashboardService.GetProductivityStatisticsAsync(userId, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting productivity statistics");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(UpcomingItemsDto), 200)]
        public async Task<IActionResult> GetUpcomingItems([FromQuery] int daysAhead = 7)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var upcomingItems = await _dashboardService.GetUpcomingItemsAsync(userId, daysAhead);
                return Ok(upcomingItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming items");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("recent-activity")]
        [ProducesResponseType(typeof(RecentActivityForDashboardDto), 200)]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 10)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var recentActivity = await _dashboardService.GetRecentActivityAsync(userId, count);
                return Ok(recentActivity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/stats")]
        [ProducesResponseType(typeof(UserStatsDto), 200)]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var stats = await _dashboardService.GetUserStatisticsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("productivity/report")]
        [ProducesResponseType(typeof(ProductivityReportDto), 200)]
        public async Task<IActionResult> GetProductivityReport(
            [FromQuery] DateTimeOffset startDate,
            [FromQuery] DateTimeOffset endDate)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var report = await _dashboardService.GetProductivityReportAsync(userId, startDate, endDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting productivity report");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("time-usage")]
        [ProducesResponseType(typeof(TimeUsageDto), 200)]
        public async Task<IActionResult> GetTimeUsageStatistics(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var timeUsage = await _dashboardService.GetTimeUsageStatisticsAsync(userId, startDate, endDate);
                return Ok(timeUsage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time usage statistics");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("notifications")]
        [ProducesResponseType(typeof(NotificationSummaryDto), 200)]
        public async Task<IActionResult> GetNotificationSummary()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var notifications = await _dashboardService.GetNotificationSummaryAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification summary");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("quick-stats")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetQuickStats()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var quickStats = await _dashboardService.GetQuickStatsAsync(userId);
                return Ok(quickStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick stats");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("category-breakdown")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetCategoryBreakdown()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var breakdown = await _dashboardService.GetCategoryBreakdownAsync(userId);
                return Ok(breakdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category breakdown");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("priority-breakdown")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetPriorityBreakdown()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var breakdown = await _dashboardService.GetPriorityBreakdownAsync(userId);
                return Ok(breakdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting priority breakdown");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("monthly-trend")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetMonthlyTrend([FromQuery] int months = 6)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var trend = await _dashboardService.GetMonthlyTrendAsync(userId, months);
                return Ok(trend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly trend");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var today = DateTimeOffset.UtcNow.Date;

                // Get multiple statistics in parallel
                var taskStatsTask = _dashboardService.GetTaskStatisticsAsync(userId);
                var appointmentStatsTask = _dashboardService.GetAppointmentStatisticsAsync(userId);
                var quickStatsTask = _dashboardService.GetQuickStatsAsync(userId);
                var upcomingTask = _dashboardService.GetUpcomingItemsAsync(userId, 3);
                var notificationsTask = _dashboardService.GetNotificationSummaryAsync(userId);

                await Task.WhenAll(
                    taskStatsTask,
                    appointmentStatsTask,
                    quickStatsTask,
                    upcomingTask,
                    notificationsTask
                );

                var summary = new
                {
                    TaskStats = await taskStatsTask,
                    AppointmentStats = await appointmentStatsTask,
                    QuickStats = await quickStatsTask,
                    UpcomingItems = await upcomingTask,
                    Notifications = await notificationsTask,
                    GeneratedAt = DateTimeOffset.UtcNow
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                return BadRequest(new { message = ex.Message });
            }
        }

        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }
    }
}