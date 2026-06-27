using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Services
{
    public class DailyStatService : IDailyStatService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskService _taskService;
        private readonly IAppointmentService _appointmentService;

        public DailyStatService(
            ApplicationDbContext context,
            ITaskService taskService,
            IAppointmentService appointmentService)
        {
            _context = context;
            _taskService = taskService;
            _appointmentService = appointmentService;
        }

        public async Task<DailyStat> GetDailyStatByIdAsync(Guid statId)
        {
            return await _context.DailyStats
                .Include(ds => ds.User)
                .FirstOrDefaultAsync(ds => ds.StatId == statId);
        }

        public async Task<DailyStat> GetDailyStatByDateAsync(Guid UserID, DateTime date)
        {
            return await _context.DailyStats
                .FirstOrDefaultAsync(ds => ds.UserID == UserID && ds.StatDate.Date == date.Date);
        }

        public async Task<IEnumerable<DailyStat>> GetUserDailyStatsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.DailyStats
                .Where(ds => ds.UserID == UserID);

            if (startDate.HasValue)
                query = query.Where(ds => ds.StatDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(ds => ds.StatDate <= endDate.Value.Date);

            return await query
                .OrderByDescending(ds => ds.StatDate)
                .ToListAsync();
        }

        public async Task<DailyStat> CreateDailyStatAsync(DailyStat dailyStat)
        {
            var existing = await GetDailyStatByDateAsync(dailyStat.UserID, dailyStat.StatDate);
            if (existing != null)
                throw new InvalidOperationException($"Daily stat already exists for date {dailyStat.StatDate:yyyy-MM-dd}");

            dailyStat.StatId = Guid.NewGuid();
            dailyStat.CalculatedAt = DateTimeOffset.UtcNow;

            _context.DailyStats.Add(dailyStat);
            await _context.SaveChangesAsync();
            return dailyStat;
        }

        public async Task<DailyStat> UpdateDailyStatAsync(DailyStat dailyStat)
        {
            var existing = await GetDailyStatByIdAsync(dailyStat.StatId);
            if (existing == null)
                throw new KeyNotFoundException($"Daily stat with ID {dailyStat.StatId} not found");

            dailyStat.CalculatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(dailyStat);
            await _context.SaveChangesAsync();
            return dailyStat;
        }

        public async Task<bool> DeleteDailyStatAsync(Guid statId)
        {
            var stat = await GetDailyStatByIdAsync(statId);
            if (stat == null) return false;

            _context.DailyStats.Remove(stat);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DailyStat> GenerateDailyStatAsync(Guid UserID, DateTime date)
        {
            if (date.Date > DateTime.Today)
                throw new ArgumentException("Cannot generate stats for future dates");

            var existing = await GetDailyStatByDateAsync(UserID, date);
            if (existing != null)
                return await RecalculateDailyStatAsync(UserID, date);

            var tasks = (await _taskService.GetTasksByDateRangeAsync(UserID, date.Date, date.Date))
                .Where(t => !t.IsDeleted)
                .ToList();

            var appointments = (await _appointmentService.GetAppointmentsByDateRangeAsync(
                UserID,
                new DateTimeOffset(date.Date),
                new DateTimeOffset(date.Date.AddDays(1).AddTicks(-1))))
                .Where(a => !a.IsDeleted)
                .ToList();

            var dailyStat = new DailyStat
            {
                UserID = UserID,
                StatDate = date.Date,
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status == "completed"),
                IncompleteTasks = tasks.Count(t => t.Status != "completed" && t.Status != "cancelled"),
                OverdueTasks = tasks.Count(t => t.Status != "completed" && t.Status != "cancelled" && t.DueDate < date.Date),
                // FIXED: Using CategoryNavigation to get category name
                PersonalTasks = tasks.Count(t => t.CategoryNavigation != null && t.CategoryNavigation.CategoryName == "Personal"),
                JobTasks = tasks.Count(t => t.CategoryNavigation != null && t.CategoryNavigation.CategoryName == "Work"),
                UnspecifiedTasks = tasks.Count(t => t.CategoryNavigation == null || t.CategoryNavigation.CategoryName == "unspecified"),
                AppointmentTasks = tasks.Count(t => t.TaskType == "appointment"),
                TotalAppointments = appointments.Count,
                CompletedAppointments = appointments.Count(a => a.Status == "completed"),
                CancelledAppointments = appointments.Count(a => a.Status == "cancelled"),
                ProductivityScore = await CalculateProductivityScoreAsync(UserID, date),
                CurrentStreakDays = await CalculateCurrentStreakAsync(UserID),
                CalculatedAt = DateTimeOffset.UtcNow
            };

            return await CreateDailyStatAsync(dailyStat);
        }

        public async Task<bool> GenerateMissingDailyStatsAsync(Guid UserID, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date must be before end date");

            var generatedCount = 0;
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                if (currentDate > DateTime.Today)
                    break;

                var existing = await GetDailyStatByDateAsync(UserID, currentDate);
                if (existing == null)
                {
                    try
                    {
                        await GenerateDailyStatAsync(UserID, currentDate);
                        generatedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error generating stat for {currentDate:yyyy-MM-dd}: {ex.Message}");
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            return generatedCount > 0;
        }

        public async Task<DailyStat> RecalculateDailyStatAsync(Guid UserID, DateTime date)
        {
            var existing = await GetDailyStatByDateAsync(UserID, date);
            if (existing == null)
                return await GenerateDailyStatAsync(UserID, date);

            await DeleteDailyStatAsync(existing.StatId);
            return await GenerateDailyStatAsync(UserID, date);
        }

        public async Task<bool> RecalculateUserStatsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var recalculatedCount = 0;
            var currentDate = startDate.Value.Date;

            while (currentDate <= endDate.Value.Date)
            {
                if (currentDate <= DateTime.Today)
                {
                    try
                    {
                        await RecalculateDailyStatAsync(UserID, currentDate);
                        recalculatedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error recalculating stat for {currentDate:yyyy-MM-dd}: {ex.Message}");
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            return recalculatedCount > 0;
        }

        public async Task<IEnumerable<DailyStat>> GetDailyStatsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.DailyStats
                .Where(ds => ds.StatDate >= startDate.Date && ds.StatDate <= endDate.Date)
                .OrderBy(ds => ds.StatDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyStat>> GetTopProductiveDaysAsync(Guid UserID, int limit = 10)
        {
            return await _context.DailyStats
                .Where(ds => ds.UserID == UserID && ds.ProductivityScore.HasValue)
                .OrderByDescending(ds => ds.ProductivityScore)
                .ThenByDescending(ds => ds.CompletedTasks)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyStat>> GetLowProductiveDaysAsync(Guid UserID, int limit = 10)
        {
            return await _context.DailyStats
                .Where(ds => ds.UserID == UserID && ds.ProductivityScore.HasValue)
                .OrderBy(ds => ds.ProductivityScore)
                .ThenBy(ds => ds.CompletedTasks)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<DailyStat> GetMostProductiveDayAsync(Guid UserID)
        {
            return await _context.DailyStats
                .Where(ds => ds.UserID == UserID && ds.ProductivityScore.HasValue)
                .OrderByDescending(ds => ds.ProductivityScore)
                .ThenByDescending(ds => ds.CompletedTasks)
                .FirstOrDefaultAsync();
        }

        public async Task<DailyStat> GetLeastProductiveDayAsync(Guid UserID)
        {
            return await _context.DailyStats
                .Where(ds => ds.UserID == UserID && ds.ProductivityScore.HasValue)
                .OrderBy(ds => ds.ProductivityScore)
                .ThenBy(ds => ds.CompletedTasks)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<DailyStat>> GetStreakDaysAsync(Guid UserID)
        {
            var stats = await GetUserDailyStatsAsync(UserID, DateTime.Today.AddDays(-30), DateTime.Today);
            var orderedStats = stats.OrderByDescending(ds => ds.StatDate).ToList();

            var streakDays = new List<DailyStat>();
            foreach (var stat in orderedStats)
            {
                if (stat.ProductivityScore >= 50)
                    streakDays.Add(stat);
                else
                    break;
            }

            return streakDays.OrderBy(ds => ds.StatDate);
        }

        public async Task<DailyStatSummaryDto> GetDailyStatSummaryAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.ToList();

            if (!statList.Any())
                return new DailyStatSummaryDto();

            return new DailyStatSummaryDto
            {
                TotalDays = statList.Count,
                TotalTasks = statList.Sum(ds => ds.TotalTasks),
                CompletedTasks = statList.Sum(ds => ds.CompletedTasks),
                TotalAppointments = statList.Sum(ds => ds.TotalAppointments),
                AverageProductivityScore = statList.Average(ds => ds.ProductivityScore ?? 0),
                BestProductivityScore = statList.Max(ds => ds.ProductivityScore ?? 0),
                WorstProductivityScore = statList.Min(ds => ds.ProductivityScore ?? 0),
                CurrentStreak = statList.OrderByDescending(ds => ds.StatDate).FirstOrDefault()?.CurrentStreakDays ?? 0,
                MostProductiveDay = statList.OrderByDescending(ds => ds.ProductivityScore).FirstOrDefault()?.StatDate,
                TaskCompletionRate = statList.Sum(ds => ds.TotalTasks) > 0
                    ? (decimal)statList.Sum(ds => ds.CompletedTasks) / statList.Sum(ds => ds.TotalTasks) * 100
                    : 0
            };
        }

        public async Task<ProductivityTrendDto> GetProductivityTrendAsync(Guid UserID, DateTime startDate, DateTime endDate)
        {
            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.OrderBy(ds => ds.StatDate).ToList();

            var trend = new ProductivityTrendDto
            {
                StartDate = startDate,
                EndDate = endDate,
                DataPoints = statList.Select(ds => new ProductivityDataPoint
                {
                    Date = ds.StatDate,
                    ProductivityScore = ds.ProductivityScore ?? 0,
                    CompletedTasks = ds.CompletedTasks,
                    TotalTasks = ds.TotalTasks
                }).ToList(),
                AverageProductivity = statList.Any() ? statList.Average(ds => ds.ProductivityScore ?? 0) : 0,
                TrendDirection = CalculateTrendDirection(statList.Select(ds => ds.ProductivityScore ?? 0).ToList())
            };

            return trend;
        }

        public async Task<WeeklySummaryDto> GetWeeklySummaryAsync(Guid UserID, DateTime weekStartDate)
        {
            var weekEndDate = weekStartDate.AddDays(6);
            var stats = await GetUserDailyStatsAsync(UserID, weekStartDate, weekEndDate);
            var statList = stats.ToList();

            var summary = new WeeklySummaryDto
            {
                WeekStartDate = weekStartDate,
                WeekEndDate = weekEndDate,
                TotalTasks = statList.Sum(ds => ds.TotalTasks),
                CompletedTasks = statList.Sum(ds => ds.CompletedTasks),
                TotalAppointments = statList.Sum(ds => ds.TotalAppointments),
                CompletedAppointments = statList.Sum(ds => ds.CompletedAppointments),
                AverageDailyProductivity = statList.Any() ? statList.Average(ds => ds.ProductivityScore ?? 0) : 0,
                MostProductiveDay = statList.OrderByDescending(ds => ds.ProductivityScore).FirstOrDefault()?.StatDate,
                DailyStats = statList.ToDictionary(
                    ds => ds.StatDate.DayOfWeek.ToString(),
                    ds => new DailySummary
                    {
                        Date = ds.StatDate,
                        ProductivityScore = ds.ProductivityScore ?? 0,
                        CompletedTasks = ds.CompletedTasks,
                        TotalTasks = ds.TotalTasks
                    })
            };

            return summary;
        }

        public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(Guid UserID, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.ToList();

            var summary = new MonthlySummaryDto
            {
                Year = year,
                Month = month,
                TotalTasks = statList.Sum(ds => ds.TotalTasks),
                CompletedTasks = statList.Sum(ds => ds.CompletedTasks),
                TotalAppointments = statList.Sum(ds => ds.TotalAppointments),
                CompletedAppointments = statList.Sum(ds => ds.CompletedAppointments),
                AverageDailyProductivity = statList.Any() ? statList.Average(ds => ds.ProductivityScore ?? 0) : 0,
                ProductiveDays = statList.Count(ds => ds.ProductivityScore >= 50),
                UnproductiveDays = statList.Count(ds => ds.ProductivityScore < 50),
                BestDay = statList.OrderByDescending(ds => ds.ProductivityScore).FirstOrDefault()?.StatDate,
                WorstDay = statList.OrderBy(ds => ds.ProductivityScore).FirstOrDefault()?.StatDate,
                TaskCompletionRate = statList.Sum(ds => ds.TotalTasks) > 0
                    ? (decimal)statList.Sum(ds => ds.CompletedTasks) / statList.Sum(ds => ds.TotalTasks) * 100
                    : 0
            };

            return summary;
        }

        public async Task<YearlySummaryDto> GetYearlySummaryAsync(Guid UserID, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.ToList();

            var monthlySummaries = new Dictionary<int, MonthlySummary>();
            for (int month = 1; month <= 12; month++)
            {
                var monthStats = statList.Where(ds => ds.StatDate.Month == month).ToList();
                monthlySummaries[month] = new MonthlySummary
                {
                    Month = month,
                    TotalTasks = monthStats.Sum(ds => ds.TotalTasks),
                    CompletedTasks = monthStats.Sum(ds => ds.CompletedTasks),
                    AverageProductivity = monthStats.Any() ? monthStats.Average(ds => ds.ProductivityScore ?? 0) : 0
                };
            }

            var summary = new YearlySummaryDto
            {
                Year = year,
                TotalTasks = statList.Sum(ds => ds.TotalTasks),
                CompletedTasks = statList.Sum(ds => ds.CompletedTasks),
                TotalAppointments = statList.Sum(ds => ds.TotalAppointments),
                CompletedAppointments = statList.Sum(ds => ds.CompletedAppointments),
                AverageMonthlyProductivity = monthlySummaries.Values.Any(m => m.AverageProductivity > 0)
                    ? monthlySummaries.Values.Where(m => m.AverageProductivity > 0).Average(m => m.AverageProductivity)
                    : 0,
                BestMonth = monthlySummaries.Values.OrderByDescending(m => m.AverageProductivity).FirstOrDefault()?.Month,
                WorstMonth = monthlySummaries.Values.Where(m => m.AverageProductivity > 0).OrderBy(m => m.AverageProductivity).FirstOrDefault()?.Month,
                MonthlySummaries = monthlySummaries,
                ProductiveMonths = monthlySummaries.Values.Count(m => m.AverageProductivity >= 50)
            };

            return summary;
        }

        public async Task<decimal> CalculateProductivityScoreAsync(Guid UserID, DateTime date)
        {
            var tasks = (await _taskService.GetTasksByDateRangeAsync(UserID, date.Date, date.Date))
                .Where(t => !t.IsDeleted)
                .ToList();

            if (!tasks.Any())
                return 0;

            var completedTasks = tasks.Count(t => t.Status == "completed");
            var totalTasks = tasks.Count;

            var completionScore = (decimal)completedTasks / totalTasks * 70;
            var highPriorityCompleted = tasks.Count(t => t.Status == "completed" && t.PriorityLevel == "high");
            var priorityBonus = Math.Min(highPriorityCompleted * 5, 20);
            var earlyCompleted = tasks.Count(t => t.Status == "completed" && t.DueDate > date.Date);
            var earlyBonus = Math.Min(earlyCompleted * 2, 10);

            var totalScore = completionScore + priorityBonus + earlyBonus;
            return Math.Min(totalScore, 100);
        }

        public async Task<int> CalculateCurrentStreakAsync(Guid UserID)
        {
            var todayStat = await GetDailyStatByDateAsync(UserID, DateTime.Today);
            var yesterdayStat = await GetDailyStatByDateAsync(UserID, DateTime.Today.AddDays(-1));

            if (todayStat?.ProductivityScore >= 50)
            {
                var streak = 1;
                var checkDate = DateTime.Today.AddDays(-1);

                while (checkDate >= DateTime.Today.AddDays(-365))
                {
                    var stat = await GetDailyStatByDateAsync(UserID, checkDate);
                    if (stat?.ProductivityScore >= 50)
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }

                return streak;
            }

            return 0;
        }

        public async Task<int> CalculateLongestStreakAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-365);
            endDate ??= DateTime.Today;

            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var orderedStats = stats.OrderBy(ds => ds.StatDate).ToList();

            var longestStreak = 0;
            var currentStreak = 0;

            foreach (var stat in orderedStats)
            {
                if (stat.ProductivityScore >= 50)
                {
                    currentStreak++;
                    longestStreak = Math.Max(longestStreak, currentStreak);
                }
                else
                {
                    currentStreak = 0;
                }
            }

            return longestStreak;
        }

        public async Task<Dictionary<string, decimal>> CalculateAverageDailyMetricsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.ToList();

            if (!statList.Any())
                return new Dictionary<string, decimal>();

            return new Dictionary<string, decimal>
            {
                { "average_tasks_per_day", (decimal)statList.Average(ds => ds.TotalTasks) },
                { "average_completed_tasks_per_day", (decimal)statList.Average(ds => ds.CompletedTasks) },
                { "average_productivity_score", (decimal)statList.Average(ds => ds.ProductivityScore ?? 0) },
                { "average_appointments_per_day", (decimal)statList.Average(ds => ds.TotalAppointments) },
                { "task_completion_rate", statList.Sum(ds => ds.TotalTasks) > 0
                    ? (decimal)statList.Sum(ds => ds.CompletedTasks) / statList.Sum(ds => ds.TotalTasks) * 100
                    : 0 }
            };
        }

        public async Task<Dictionary<string, object>> CalculatePerformanceMetricsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.ToList();

            if (!statList.Any())
                return new Dictionary<string, object>();

            var productiveDays = statList.Count(ds => ds.ProductivityScore >= 50);
            var totalDays = statList.Count;

            return new Dictionary<string, object>
            {
                { "total_days_analyzed", totalDays },
                { "productive_days", productiveDays },
                { "unproductive_days", totalDays - productiveDays },
                { "productivity_percentage", totalDays > 0 ? (decimal)productiveDays / totalDays * 100 : 0 },
                { "best_day", statList.OrderByDescending(ds => ds.ProductivityScore).FirstOrDefault()?.StatDate },
                { "worst_day", statList.OrderBy(ds => ds.ProductivityScore).FirstOrDefault()?.StatDate },
                { "current_streak", await CalculateCurrentStreakAsync(UserID) },
                { "longest_streak", await CalculateLongestStreakAsync(UserID, startDate, endDate) },
                { "average_daily_tasks", statList.Average(ds => ds.TotalTasks) },
                { "average_daily_completed", statList.Average(ds => ds.CompletedTasks) }
            };
        }

        public async Task<ComparisonResultDto> CompareDaysAsync(Guid UserID, DateTime date1, DateTime date2)
        {
            var stat1 = await GetDailyStatByDateAsync(UserID, date1) ?? await GenerateDailyStatAsync(UserID, date1);
            var stat2 = await GetDailyStatByDateAsync(UserID, date2) ?? await GenerateDailyStatAsync(UserID, date2);

            return new ComparisonResultDto
            {
                Date1 = date1,
                Date2 = date2,
                ProductivityScore1 = stat1.ProductivityScore ?? 0,
                ProductivityScore2 = stat2.ProductivityScore ?? 0,
                CompletedTasks1 = stat1.CompletedTasks,
                CompletedTasks2 = stat2.CompletedTasks,
                TotalTasks1 = stat1.TotalTasks,
                TotalTasks2 = stat2.TotalTasks,
                ProductivityDifference = (stat2.ProductivityScore ?? 0) - (stat1.ProductivityScore ?? 0),
                IsDate2Better = (stat2.ProductivityScore ?? 0) > (stat1.ProductivityScore ?? 0)
            };
        }

        public async Task<ComparisonResultDto> CompareWeeksAsync(Guid UserID, DateTime week1Start, DateTime week2Start)
        {
            var week1End = week1Start.AddDays(6);
            var week2End = week2Start.AddDays(6);

            var week1Stats = await GetUserDailyStatsAsync(UserID, week1Start, week1End);
            var week2Stats = await GetUserDailyStatsAsync(UserID, week2Start, week2End);

            var week1Avg = week1Stats.Any() ? week1Stats.Average(ds => ds.ProductivityScore ?? 0) : 0;
            var week2Avg = week2Stats.Any() ? week2Stats.Average(ds => ds.ProductivityScore ?? 0) : 0;

            return new ComparisonResultDto
            {
                Date1 = week1Start,
                Date2 = week2Start,
                ProductivityScore1 = week1Avg,
                ProductivityScore2 = week2Avg,
                CompletedTasks1 = week1Stats.Sum(ds => ds.CompletedTasks),
                CompletedTasks2 = week2Stats.Sum(ds => ds.CompletedTasks),
                TotalTasks1 = week1Stats.Sum(ds => ds.TotalTasks),
                TotalTasks2 = week2Stats.Sum(ds => ds.TotalTasks),
                ProductivityDifference = week2Avg - week1Avg,
                IsDate2Better = week2Avg > week1Avg
            };
        }

        public async Task<ComparisonResultDto> CompareMonthsAsync(Guid UserID, int year1, int month1, int year2, int month2)
        {
            var month1Start = new DateTime(year1, month1, 1);
            var month1End = month1Start.AddMonths(1).AddDays(-1);
            var month2Start = new DateTime(year2, month2, 1);
            var month2End = month2Start.AddMonths(1).AddDays(-1);

            var month1Stats = await GetUserDailyStatsAsync(UserID, month1Start, month1End);
            var month2Stats = await GetUserDailyStatsAsync(UserID, month2Start, month2End);

            var month1Avg = month1Stats.Any() ? month1Stats.Average(ds => ds.ProductivityScore ?? 0) : 0;
            var month2Avg = month2Stats.Any() ? month2Stats.Average(ds => ds.ProductivityScore ?? 0) : 0;

            return new ComparisonResultDto
            {
                Date1 = month1Start,
                Date2 = month2Start,
                ProductivityScore1 = month1Avg,
                ProductivityScore2 = month2Avg,
                CompletedTasks1 = month1Stats.Sum(ds => ds.CompletedTasks),
                CompletedTasks2 = month2Stats.Sum(ds => ds.CompletedTasks),
                TotalTasks1 = month1Stats.Sum(ds => ds.TotalTasks),
                TotalTasks2 = month2Stats.Sum(ds => ds.TotalTasks),
                ProductivityDifference = month2Avg - month1Avg,
                IsDate2Better = month2Avg > month1Avg
            };
        }

        public async Task<TrendAnalysisDto> AnalyzeProductivityTrendAsync(Guid UserID, DateTime startDate, DateTime endDate)
        {
            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.OrderBy(ds => ds.StatDate).ToList();

            if (statList.Count < 2)
                return new TrendAnalysisDto { HasEnoughData = false };

            var productivityScores = statList.Select(ds => ds.ProductivityScore ?? 0).ToList();
            var xValues = Enumerable.Range(0, productivityScores.Count).Select(i => (double)i).ToArray();
            var yValues = productivityScores.Select(s => (double)s).ToArray();

            var n = xValues.Length;
            var sumX = xValues.Sum();
            var sumY = yValues.Sum();
            var sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
            var sumX2 = xValues.Sum(x => x * x);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            var yMean = yValues.Average();
            var ssTot = yValues.Sum(y => Math.Pow(y - yMean, 2));
            var ssRes = yValues.Zip(xValues, (y, x) => Math.Pow(y - (slope * x + intercept), 2)).Sum();
            var rSquared = 1 - (ssRes / ssTot);

            return new TrendAnalysisDto
            {
                StartDate = startDate,
                EndDate = endDate,
                HasEnoughData = true,
                TrendSlope = slope,
                TrendStrength = Math.Abs(slope) > 0.5 ? "Strong" : Math.Abs(slope) > 0.2 ? "Moderate" : "Weak",
                TrendDirection = slope > 0 ? "Improving" : slope < 0 ? "Declining" : "Stable",
                RSquared = rSquared,
                AverageProductivity = productivityScores.Average(),
                DataPoints = statList.Select(ds => new TrendDataPoint
                {
                    Date = ds.StatDate,
                    ProductivityScore = ds.ProductivityScore ?? 0,
                    PredictedScore = (decimal)(slope * Array.IndexOf(xValues, xValues.First()) + intercept)
                }).ToList()
            };
        }

        public async Task<bool> CheckDailyGoalAsync(Guid UserID, DateTime date, int targetTasks = 5)
        {
            var stat = await GetDailyStatByDateAsync(UserID, date) ?? await GenerateDailyStatAsync(UserID, date);
            return stat.CompletedTasks >= targetTasks;
        }

        public async Task<GoalProgressDto> GetGoalProgressAsync(Guid UserID, DateTime startDate, DateTime endDate, int targetTasksPerDay)
        {
            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.ToList();

            var totalDays = (endDate - startDate).Days + 1;
            var goalAchievedDays = statList.Count(ds => ds.CompletedTasks >= targetTasksPerDay);
            var totalTasksGoal = totalDays * targetTasksPerDay;
            var actualCompletedTasks = statList.Sum(ds => ds.CompletedTasks);

            return new GoalProgressDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TargetTasksPerDay = targetTasksPerDay,
                TotalDays = totalDays,
                GoalAchievedDays = goalAchievedDays,
                GoalSuccessRate = totalDays > 0 ? (decimal)goalAchievedDays / totalDays * 100 : 0,
                TotalTasksGoal = totalTasksGoal,
                ActualCompletedTasks = actualCompletedTasks,
                GoalCompletionPercentage = totalTasksGoal > 0 ? (decimal)actualCompletedTasks / totalTasksGoal * 100 : 0,
                AverageDailyCompleted = statList.Any() ? (decimal)statList.Average(ds => ds.CompletedTasks) : 0,
            };
        }

        public async Task<IEnumerable<DateTime>> GetGoalAchievedDaysAsync(Guid UserID, DateTime startDate, DateTime endDate, int targetTasksPerDay)
        {
            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            return stats
                .Where(ds => ds.CompletedTasks >= targetTasksPerDay)
                .Select(ds => ds.StatDate)
                .OrderBy(d => d)
                .ToList();
        }

        public async Task<bool> GenerateAllUserStatsForDateAsync(DateTime date)
        {
            if (date > DateTime.Today)
                return false;

            var users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            var generatedCount = 0;
            foreach (var user in users)
            {
                try
                {
                    await GenerateDailyStatAsync(user.UserID, date);
                    generatedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating stat for user {user.UserID} on {date:yyyy-MM-dd}: {ex.Message}");
                }
            }

            return generatedCount > 0;
        }

        public async Task<bool> RecalculateAllStatsForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate || endDate > DateTime.Today)
                return false;

            var users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            var recalculatedCount = 0;
            foreach (var user in users)
            {
                try
                {
                    await RecalculateUserStatsAsync(user.UserID, startDate, endDate);
                    recalculatedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error recalculating stats for user {user.UserID}: {ex.Message}");
                }
            }

            return recalculatedCount > 0;
        }

        public async Task<bool> CleanupOldStatsAsync(int retentionDays = 365)
        {
            var cutoffDate = DateTime.Today.AddDays(-retentionDays);
            var oldStats = await _context.DailyStats
                .Where(ds => ds.StatDate < cutoffDate)
                .ToListAsync();

            if (!oldStats.Any())
                return false;

            _context.DailyStats.RemoveRange(oldStats);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportDailyStatsToCsvAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.OrderBy(ds => ds.StatDate).ToList();

            var csvLines = new List<string>
            {
                "Date,Total Tasks,Completed Tasks,Incomplete Tasks,Overdue Tasks,Total Appointments,Completed Appointments,Productivity Score,Streak Days"
            };

            foreach (var stat in statList)
            {
                csvLines.Add($"{stat.StatDate:yyyy-MM-dd},{stat.TotalTasks},{stat.CompletedTasks},{stat.IncompleteTasks},{stat.OverdueTasks},{stat.TotalAppointments},{stat.CompletedAppointments},{stat.ProductivityScore ?? 0},{stat.CurrentStreakDays}");
            }

            return System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, csvLines));
        }

        public async Task<string> ExportDailyStatsToJsonAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            var stats = await GetUserDailyStatsAsync(UserID, startDate, endDate);
            var statList = stats.OrderBy(ds => ds.StatDate).ToList();

            var exportData = new
            {
                UserID = UserID,
                ExportDate = DateTime.UtcNow,
                DateRange = new { Start = startDate, End = endDate },
                Statistics = statList.Select(stat => new
                {
                    Date = stat.StatDate.ToString("yyyy-MM-dd"),
                    stat.TotalTasks,
                    stat.CompletedTasks,
                    stat.IncompleteTasks,
                    stat.OverdueTasks,
                    stat.TotalAppointments,
                    stat.CompletedAppointments,
                    ProductivityScore = stat.ProductivityScore ?? 0,
                    stat.CurrentStreakDays
                }),
                Summary = new
                {
                    TotalDays = statList.Count,
                    TotalTasks = statList.Sum(ds => ds.TotalTasks),
                    TotalCompletedTasks = statList.Sum(ds => ds.CompletedTasks),
                    AverageProductivity = statList.Any() ? statList.Average(ds => ds.ProductivityScore ?? 0) : 0
                }
            };

            return System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<bool> DailyStatExistsAsync(Guid UserID, DateTime date)
        {
            return await GetDailyStatByDateAsync(UserID, date) != null;
        }

        public async Task<bool> IsDateInFutureAsync(DateTime date)
        {
            return date.Date > DateTime.Today;
        }

        private string CalculateTrendDirection(List<decimal> scores)
        {
            if (scores.Count < 2)
                return "insufficient_data";

            var firstHalf = scores.Take(scores.Count / 2).Average();
            var secondHalf = scores.Skip(scores.Count / 2).Average();

            if (secondHalf > firstHalf * 1.1m)
                return "improving";
            else if (secondHalf < firstHalf * 0.9m)
                return "declining";
            else
                return "stable";
        }
    }
}