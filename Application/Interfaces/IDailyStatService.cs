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
        Task<DailyStat> GetDailyStatByDateAsync(Guid UserID, DateTime date);
        Task<IEnumerable<DailyStat>> GetUserDailyStatsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<DailyStat> CreateDailyStatAsync(DailyStat dailyStat);
        Task<DailyStat> UpdateDailyStatAsync(DailyStat dailyStat);
        Task<bool> DeleteDailyStatAsync(Guid statId);

        // Daily operations
        Task<DailyStat> GenerateDailyStatAsync(Guid UserID, DateTime date);
        Task<bool> GenerateMissingDailyStatsAsync(Guid UserID, DateTime startDate, DateTime endDate);
        Task<DailyStat> RecalculateDailyStatAsync(Guid UserID, DateTime date);
        Task<bool> RecalculateUserStatsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);

        // Query operations
        Task<IEnumerable<DailyStat>> GetDailyStatsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DailyStat>> GetTopProductiveDaysAsync(Guid UserID, int limit = 10);
        Task<IEnumerable<DailyStat>> GetLowProductiveDaysAsync(Guid UserID, int limit = 10);
        Task<DailyStat> GetMostProductiveDayAsync(Guid UserID);
        Task<DailyStat> GetLeastProductiveDayAsync(Guid UserID);
        Task<IEnumerable<DailyStat>> GetStreakDaysAsync(Guid UserID);

        // Statistics and aggregates
        Task<DailyStatSummaryDto> GetDailyStatSummaryAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<ProductivityTrendDto> GetProductivityTrendAsync(Guid UserID, DateTime startDate, DateTime endDate);
        Task<WeeklySummaryDto> GetWeeklySummaryAsync(Guid UserID, DateTime weekStartDate);
        Task<MonthlySummaryDto> GetMonthlySummaryAsync(Guid UserID, int year, int month);
        Task<YearlySummaryDto> GetYearlySummaryAsync(Guid UserID, int year);

        // Metrics calculations
        Task<decimal> CalculateProductivityScoreAsync(Guid UserID, DateTime date);
        Task<int> CalculateCurrentStreakAsync(Guid UserID);
        Task<int> CalculateLongestStreakAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> CalculateAverageDailyMetricsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> CalculatePerformanceMetricsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);

        // Comparison and analysis
        Task<ComparisonResultDto> CompareDaysAsync(Guid UserID, DateTime date1, DateTime date2);
        Task<ComparisonResultDto> CompareWeeksAsync(Guid UserID, DateTime week1Start, DateTime week2Start);
        Task<ComparisonResultDto> CompareMonthsAsync(Guid UserID, int year1, int month1, int year2, int month2);
        Task<TrendAnalysisDto> AnalyzeProductivityTrendAsync(Guid UserID, DateTime startDate, DateTime endDate);

        // Goals and targets
        Task<bool> CheckDailyGoalAsync(Guid UserID, DateTime date, int targetTasks = 5);
        Task<GoalProgressDto> GetGoalProgressAsync(Guid UserID, DateTime startDate, DateTime endDate, int targetTasksPerDay);
        Task<IEnumerable<DateTime>> GetGoalAchievedDaysAsync(Guid UserID, DateTime startDate, DateTime endDate, int targetTasksPerDay);

        // Bulk operations
        Task<bool> GenerateAllUserStatsForDateAsync(DateTime date);
        Task<bool> RecalculateAllStatsForDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> CleanupOldStatsAsync(int retentionDays = 365);

        // Export
        Task<byte[]> ExportDailyStatsToCsvAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<string> ExportDailyStatsToJsonAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);

        // Validation
        Task<bool> DailyStatExistsAsync(Guid UserID, DateTime date);
        Task<bool> IsDateInFutureAsync(DateTime date);
    }
}