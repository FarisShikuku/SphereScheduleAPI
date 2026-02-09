using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IDailyStatService
    {
        // Basic CRUD
        Task<DailyStat> GetDailyStatByIdAsync(Guid statId);
        Task<DailyStat> GetDailyStatByDateAsync(Guid userId, DateTime date);
        Task<IEnumerable<DailyStat>> GetUserDailyStatsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<DailyStat> CreateDailyStatAsync(DailyStat dailyStat);
        Task<DailyStat> UpdateDailyStatAsync(DailyStat dailyStat);
        Task<bool> DeleteDailyStatAsync(Guid statId);

        // Daily operations
        Task<DailyStat> GenerateDailyStatAsync(Guid userId, DateTime date);
        Task<bool> GenerateMissingDailyStatsAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<DailyStat> RecalculateDailyStatAsync(Guid userId, DateTime date);
        Task<bool> RecalculateUserStatsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);

        // Query operations
        Task<IEnumerable<DailyStat>> GetDailyStatsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DailyStat>> GetTopProductiveDaysAsync(Guid userId, int limit = 10);
        Task<IEnumerable<DailyStat>> GetLowProductiveDaysAsync(Guid userId, int limit = 10);
        Task<DailyStat> GetMostProductiveDayAsync(Guid userId);
        Task<DailyStat> GetLeastProductiveDayAsync(Guid userId);
        Task<IEnumerable<DailyStat>> GetStreakDaysAsync(Guid userId);

        // Statistics and aggregates
        Task<DailyStatSummaryDto> GetDailyStatSummaryAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ProductivityTrendDto> GetProductivityTrendAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<WeeklySummaryDto> GetWeeklySummaryAsync(Guid userId, DateTime weekStartDate);
        Task<MonthlySummaryDto> GetMonthlySummaryAsync(Guid userId, int year, int month);
        Task<YearlySummaryDto> GetYearlySummaryAsync(Guid userId, int year);

        // Metrics calculations
        Task<decimal> CalculateProductivityScoreAsync(Guid userId, DateTime date);
        Task<int> CalculateCurrentStreakAsync(Guid userId);
        Task<int> CalculateLongestStreakAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> CalculateAverageDailyMetricsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> CalculatePerformanceMetricsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);

        // Comparison and analysis
        Task<ComparisonResultDto> CompareDaysAsync(Guid userId, DateTime date1, DateTime date2);
        Task<ComparisonResultDto> CompareWeeksAsync(Guid userId, DateTime week1Start, DateTime week2Start);
        Task<ComparisonResultDto> CompareMonthsAsync(Guid userId, int year1, int month1, int year2, int month2);
        Task<TrendAnalysisDto> AnalyzeProductivityTrendAsync(Guid userId, DateTime startDate, DateTime endDate);

        // Goals and targets
        Task<bool> CheckDailyGoalAsync(Guid userId, DateTime date, int targetTasks = 5);
        Task<GoalProgressDto> GetGoalProgressAsync(Guid userId, DateTime startDate, DateTime endDate, int targetTasksPerDay);
        Task<IEnumerable<DateTime>> GetGoalAchievedDaysAsync(Guid userId, DateTime startDate, DateTime endDate, int targetTasksPerDay);

        // Bulk operations
        Task<bool> GenerateAllUserStatsForDateAsync(DateTime date);
        Task<bool> RecalculateAllStatsForDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> CleanupOldStatsAsync(int retentionDays = 365);

        // Export
        Task<byte[]> ExportDailyStatsToCsvAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<string> ExportDailyStatsToJsonAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);

        // Validation
        Task<bool> DailyStatExistsAsync(Guid userId, DateTime date);
        Task<bool> IsDateInFutureAsync(DateTime date);
    }
}