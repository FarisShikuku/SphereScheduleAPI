using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.Mappings;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SphereScheduleAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DailyStatsController : ControllerBase
    {
        private readonly IDailyStatService _dailyStatService;
        private readonly IMapper _mapper;

        public DailyStatsController(IDailyStatService dailyStatService, IMapper mapper)
        {
            _dailyStatService = dailyStatService;
            _mapper = mapper;
        }

        private Guid GetCurrentUserID()
        {
            var UserIDClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(UserIDClaim) || !Guid.TryParse(UserIDClaim, out var UserID))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return UserID;
        }

        // GET: api/dailystats
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyStatDto>>> GetDailyStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var stats = await _dailyStatService.GetUserDailyStatsAsync(UserID, startDate, endDate);
                return Ok(_mapper.Map<IEnumerable<DailyStatDto>>(stats));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving daily stats", error = ex.Message });
            }
        }

        // GET: api/dailystats/today
        [HttpGet("today")]
        public async Task<ActionResult<DailyStatDto>> GetTodayStats()
        {
            try
            {
                var UserID = GetCurrentUserID();
                var today = DateTime.Today;
                var stat = await _dailyStatService.GetDailyStatByDateAsync(UserID, today);

                if (stat == null)
                {
                    // Generate today's stat if it doesn't exist
                    stat = await _dailyStatService.GenerateDailyStatAsync(UserID, today);
                }

                return Ok(_mapper.Map<DailyStatDto>(stat));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving today's stats", error = ex.Message });
            }
        }

        // GET: api/dailystats/date/{date}
        [HttpGet("date/{date}")]
        public async Task<ActionResult<DailyStatDto>> GetStatsByDate(DateTime date)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (await _dailyStatService.IsDateInFutureAsync(date))
                    return BadRequest(new { message = "Cannot get stats for future dates" });

                var stat = await _dailyStatService.GetDailyStatByDateAsync(UserID, date);

                if (stat == null)
                {
                    // Generate stat if it doesn't exist
                    stat = await _dailyStatService.GenerateDailyStatAsync(UserID, date);
                }

                return Ok(_mapper.Map<DailyStatDto>(stat));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving stats for date", error = ex.Message });
            }
        }

        // POST: api/dailystats/generate
        [HttpPost("generate")]
        public async Task<ActionResult<DailyStatDto>> GenerateDailyStat([FromBody] GenerateDailyStatDto generateDto)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (await _dailyStatService.IsDateInFutureAsync(generateDto.Date))
                    return BadRequest(new { message = "Cannot generate stats for future dates" });

                DailyStat stat;
                if (generateDto.ForceRecalculate)
                {
                    stat = await _dailyStatService.RecalculateDailyStatAsync(UserID, generateDto.Date);
                }
                else
                {
                    stat = await _dailyStatService.GenerateDailyStatAsync(UserID, generateDto.Date);
                }

                return Ok(_mapper.Map<DailyStatDto>(stat));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating daily stat", error = ex.Message });
            }
        }

        // POST: api/dailystats/generate-range
        [HttpPost("generate-range")]
        public async Task<ActionResult> GenerateDailyStatsForRange([FromBody] DateRangeDto rangeDto)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (rangeDto.StartDate > rangeDto.EndDate)
                    return BadRequest(new { message = "Start date must be before end date" });

                if (await _dailyStatService.IsDateInFutureAsync(rangeDto.EndDate))
                    return BadRequest(new { message = "Cannot generate stats for future dates" });

                var success = await _dailyStatService.GenerateMissingDailyStatsAsync(UserID, rangeDto.StartDate, rangeDto.EndDate);

                if (!success)
                    return StatusCode(500, new { message = "Failed to generate stats for date range" });

                return Ok(new { message = "Daily stats generated successfully for specified range" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating daily stats for range", error = ex.Message });
            }
        }

        // GET: api/dailystats/summary
        [HttpGet("summary")]
        public async Task<ActionResult<DailyStatSummaryDto>> GetSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var summary = await _dailyStatService.GetDailyStatSummaryAsync(UserID, startDate, endDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving daily stats summary", error = ex.Message });
            }
        }

        // GET: api/dailystats/trend
        [HttpGet("trend")]
        public async Task<ActionResult<ProductivityTrendDto>> GetProductivityTrend(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (startDate > endDate)
                    return BadRequest(new { message = "Start date must be before end date" });

                var trend = await _dailyStatService.GetProductivityTrendAsync(UserID, startDate, endDate);
                return Ok(trend);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving productivity trend", error = ex.Message });
            }
        }

        // GET: api/dailystats/weekly/{weekStartDate}
        [HttpGet("weekly/{weekStartDate}")]
        public async Task<ActionResult<WeeklySummaryDto>> GetWeeklySummary(DateTime weekStartDate)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var summary = await _dailyStatService.GetWeeklySummaryAsync(UserID, weekStartDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving weekly summary", error = ex.Message });
            }
        }

        // GET: api/dailystats/monthly/{year}/{month}
        [HttpGet("monthly/{year}/{month}")]
        public async Task<ActionResult<MonthlySummaryDto>> GetMonthlySummary(int year, int month)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var summary = await _dailyStatService.GetMonthlySummaryAsync(UserID, year, month);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving monthly summary", error = ex.Message });
            }
        }

        // GET: api/dailystats/yearly/{year}
        [HttpGet("yearly/{year}")]
        public async Task<ActionResult<YearlySummaryDto>> GetYearlySummary(int year)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var summary = await _dailyStatService.GetYearlySummaryAsync(UserID, year);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving yearly summary", error = ex.Message });
            }
        }

        // GET: api/dailystats/top-productive
        [HttpGet("top-productive")]
        public async Task<ActionResult<IEnumerable<DailyStatDto>>> GetTopProductiveDays([FromQuery] int limit = 10)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var stats = await _dailyStatService.GetTopProductiveDaysAsync(UserID, limit);
                return Ok(_mapper.Map<IEnumerable<DailyStatDto>>(stats));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving top productive days", error = ex.Message });
            }
        }

        // GET: api/dailystats/streak
        [HttpGet("streak")]
        public async Task<ActionResult<StreakInfoDto>> GetStreakInfo()
        {
            try
            {
                var UserID = GetCurrentUserID();

                var currentStreak = await _dailyStatService.CalculateCurrentStreakAsync(UserID);
                var longestStreak = await _dailyStatService.CalculateLongestStreakAsync(UserID);
                var streakDays = await _dailyStatService.GetStreakDaysAsync(UserID);

                var streakInfo = new StreakInfoDto
                {
                    CurrentStreak = currentStreak,
                    LongestStreak = longestStreak,
                    StreakStartDate = streakDays.Any() ? streakDays.Min(ds => ds.StatDate) : null,
                    StreakEndDate = streakDays.Any() ? streakDays.Max(ds => ds.StatDate) : null,
                    StreakDays = streakDays.Select(ds => ds.StatDate).ToList()
                };

                return Ok(streakInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving streak info", error = ex.Message });
            }
        }

        // GET: api/dailystats/compare
        [HttpGet("compare")]
        public async Task<ActionResult<ComparisonResultDto>> CompareDates(
            [FromQuery] DateTime date1,
            [FromQuery] DateTime date2)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (await _dailyStatService.IsDateInFutureAsync(date1) || await _dailyStatService.IsDateInFutureAsync(date2))
                    return BadRequest(new { message = "Cannot compare future dates" });

                var comparison = await _dailyStatService.CompareDaysAsync(UserID, date1, date2);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error comparing dates", error = ex.Message });
            }
        }

        // GET: api/dailystats/compare-weeks
        [HttpGet("compare-weeks")]
        public async Task<ActionResult<ComparisonResultDto>> CompareWeeks(
            [FromQuery] DateTime week1Start,
            [FromQuery] DateTime week2Start)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var comparison = await _dailyStatService.CompareWeeksAsync(UserID, week1Start, week2Start);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error comparing weeks", error = ex.Message });
            }
        }

        // GET: api/dailystats/compare-months
        [HttpGet("compare-months")]
        public async Task<ActionResult<ComparisonResultDto>> CompareMonths(
            [FromQuery] int year1,
            [FromQuery] int month1,
            [FromQuery] int year2,
            [FromQuery] int month2)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var comparison = await _dailyStatService.CompareMonthsAsync(UserID, year1, month1, year2, month2);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error comparing months", error = ex.Message });
            }
        }

        // GET: api/dailystats/trend-analysis
        [HttpGet("trend-analysis")]
        public async Task<ActionResult<TrendAnalysisDto>> AnalyzeTrend(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (startDate > endDate)
                    return BadRequest(new { message = "Start date must be before end date" });

                var analysis = await _dailyStatService.AnalyzeProductivityTrendAsync(UserID, startDate, endDate);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error analyzing trend", error = ex.Message });
            }
        }

        // GET: api/dailystats/goal-progress
        [HttpGet("goal-progress")]
        public async Task<ActionResult<GoalProgressDto>> GetGoalProgress(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int targetTasksPerDay = 5)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (startDate > endDate)
                    return BadRequest(new { message = "Start date must be before end date" });

                var progress = await _dailyStatService.GetGoalProgressAsync(UserID, startDate, endDate, targetTasksPerDay);
                return Ok(progress);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving goal progress", error = ex.Message });
            }
        }

        // GET: api/dailystats/performance-metrics
        [HttpGet("performance-metrics")]
        public async Task<ActionResult<object>> GetPerformanceMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var metrics = await _dailyStatService.CalculatePerformanceMetricsAsync(UserID, startDate, endDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving performance metrics", error = ex.Message });
            }
        }

        // GET: api/dailystats/averages
        [HttpGet("averages")]
        public async Task<ActionResult<object>> GetAverageMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var averages = await _dailyStatService.CalculateAverageDailyMetricsAsync(UserID, startDate, endDate);
                return Ok(averages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving average metrics", error = ex.Message });
            }
        }

        // GET: api/dailystats/export/csv
        [HttpGet("export/csv")]
        public async Task<ActionResult> ExportToCsv(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var csvData = await _dailyStatService.ExportDailyStatsToCsvAsync(UserID, startDate, endDate);

                var fileName = $"dailystats-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                return File(csvData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting to CSV", error = ex.Message });
            }
        }

        // GET: api/dailystats/export/json
        [HttpGet("export/json")]
        public async Task<ActionResult> ExportToJson(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var jsonData = await _dailyStatService.ExportDailyStatsToJsonAsync(UserID, startDate, endDate);

                var fileName = $"dailystats-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting to JSON", error = ex.Message });
            }
        }

        // POST: api/dailystats/recalculate
        [HttpPost("recalculate")]
        public async Task<ActionResult> RecalculateStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var UserID = GetCurrentUserID();
                var success = await _dailyStatService.RecalculateUserStatsAsync(UserID, startDate, endDate);

                if (!success)
                    return StatusCode(500, new { message = "Failed to recalculate stats" });

                return Ok(new { message = "Stats recalculated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error recalculating stats", error = ex.Message });
            }
        }

        // GET: api/dailystats/check-goal/{date}
        [HttpGet("check-goal/{date}")]
        public async Task<ActionResult> CheckDailyGoal(DateTime date, [FromQuery] int targetTasks = 5)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (await _dailyStatService.IsDateInFutureAsync(date))
                    return BadRequest(new { message = "Cannot check goal for future dates" });

                var achieved = await _dailyStatService.CheckDailyGoalAsync(UserID, date, targetTasks);
                return Ok(new { date, targetTasks, achieved });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking daily goal", error = ex.Message });
            }
        }

        // GET: api/dailystats/goal-achieved-days
        [HttpGet("goal-achieved-days")]
        public async Task<ActionResult<IEnumerable<DateTime>>> GetGoalAchievedDays(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int targetTasksPerDay = 5)
        {
            try
            {
                var UserID = GetCurrentUserID();

                if (startDate > endDate)
                    return BadRequest(new { message = "Start date must be before end date" });

                var achievedDays = await _dailyStatService.GetGoalAchievedDaysAsync(UserID, startDate, endDate, targetTasksPerDay);
                return Ok(achievedDays);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving goal achieved days", error = ex.Message });
            }
        }

        // GET: api/dailystats/most-productive
        [HttpGet("most-productive")]
        public async Task<ActionResult<DailyStatDto>> GetMostProductiveDay()
        {
            try
            {
                var UserID = GetCurrentUserID();
                var stat = await _dailyStatService.GetMostProductiveDayAsync(UserID);

                if (stat == null)
                    return NotFound(new { message = "No productive days found" });

                return Ok(_mapper.Map<DailyStatDto>(stat));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving most productive day", error = ex.Message });
            }
        }

        // GET: api/dailystats/least-productive
        [HttpGet("least-productive")]
        public async Task<ActionResult<DailyStatDto>> GetLeastProductiveDay()
        {
            try
            {
                var UserID = GetCurrentUserID();
                var stat = await _dailyStatService.GetLeastProductiveDayAsync(UserID);

                if (stat == null)
                    return NotFound(new { message = "No stats found" });

                return Ok(_mapper.Map<DailyStatDto>(stat));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving least productive day", error = ex.Message });
            }
        }
    }
}