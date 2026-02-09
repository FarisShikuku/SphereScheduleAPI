using System;
using System.Collections.Generic;

namespace SphereScheduleAPI.Application.DTOs
{
    public class DailyStatDto
    {
        public Guid StatId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StatDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int IncompleteTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int PersonalTasks { get; set; }
        public int JobTasks { get; set; }
        public int UnspecifiedTasks { get; set; }
        public int AppointmentTasks { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public decimal? ProductivityScore { get; set; }
        public int CurrentStreakDays { get; set; }
        public DateTimeOffset CalculatedAt { get; set; }
        public decimal TaskCompletionRate => TotalTasks > 0 ? (decimal)CompletedTasks / TotalTasks * 100 : 0;
        public string ProductivityLevel => ProductivityScore switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Average",
            >= 20 => "Poor",
            _ => "Very Poor"
        };
    }

    public class DailyStatSummaryDto
    {
        public int TotalDays { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalAppointments { get; set; }
        public decimal AverageProductivityScore { get; set; }
        public decimal BestProductivityScore { get; set; }
        public decimal WorstProductivityScore { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime? MostProductiveDay { get; set; }
        public decimal TaskCompletionRate { get; set; }
        public int ProductiveDays { get; set; }
        public int UnproductiveDays { get; set; }
    }

    public class ProductivityTrendDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ProductivityDataPoint> DataPoints { get; set; } = new();
        public decimal AverageProductivity { get; set; }
        public string TrendDirection { get; set; } // improving, declining, stable
        public decimal TrendStrength { get; set; } // 0-1
    }

    public class ProductivityDataPoint
    {
        public DateTime Date { get; set; }
        public decimal ProductivityScore { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalTasks { get; set; }
    }

    public class WeeklySummaryDto
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal AverageDailyProductivity { get; set; }
        public DateTime? MostProductiveDay { get; set; }
        public Dictionary<string, DailySummary> DailyStats { get; set; } = new();
    }

    public class DailySummary
    {
        public DateTime Date { get; set; }
        public decimal ProductivityScore { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalTasks { get; set; }
    }

    public class MonthlySummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal AverageDailyProductivity { get; set; }
        public int ProductiveDays { get; set; }
        public int UnproductiveDays { get; set; }
        public DateTime? BestDay { get; set; }
        public DateTime? WorstDay { get; set; }
        public decimal TaskCompletionRate { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
    }

    public class YearlySummaryDto
    {
        public int Year { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal AverageMonthlyProductivity { get; set; }
        public int? BestMonth { get; set; }
        public int? WorstMonth { get; set; }
        public Dictionary<int, MonthlySummary> MonthlySummaries { get; set; } = new();
        public int ProductiveMonths { get; set; }
    }

    public class MonthlySummary
    {
        public int Month { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal AverageProductivity { get; set; }
    }

    public class ComparisonResultDto
    {
        public DateTime Date1 { get; set; }
        public DateTime Date2 { get; set; }
        public decimal ProductivityScore1 { get; set; }
        public decimal ProductivityScore2 { get; set; }
        public int CompletedTasks1 { get; set; }
        public int CompletedTasks2 { get; set; }
        public int TotalTasks1 { get; set; }
        public int TotalTasks2 { get; set; }
        public decimal ProductivityDifference { get; set; }
        public bool IsDate2Better { get; set; }
        public string ComparisonSummary => IsDate2Better
            ? $"Productivity improved by {ProductivityDifference:F1} points"
            : $"Productivity decreased by {Math.Abs(ProductivityDifference):F1} points";
    }

    public class TrendAnalysisDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool HasEnoughData { get; set; }
        public double TrendSlope { get; set; }
        public string TrendDirection { get; set; } // Improving, Declining, Stable
        public string TrendStrength { get; set; } // Strong, Moderate, Weak
        public double RSquared { get; set; }
        public decimal AverageProductivity { get; set; }
        public List<TrendDataPoint> DataPoints { get; set; } = new();
    }

    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public decimal ProductivityScore { get; set; }
        public decimal PredictedScore { get; set; }
        public decimal Residual => ProductivityScore - PredictedScore;
    }

    public class GoalProgressDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TargetTasksPerDay { get; set; }
        public int TotalDays { get; set; }
        public int GoalAchievedDays { get; set; }
        public decimal GoalSuccessRate { get; set; }
        public int TotalTasksGoal { get; set; }
        public int ActualCompletedTasks { get; set; }
        public decimal GoalCompletionPercentage { get; set; }
        public decimal AverageDailyCompleted { get; set; }
        public bool IsOnTrack => GoalCompletionPercentage >= 100;
    }

    public class GenerateDailyStatDto
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public bool ForceRecalculate { get; set; } = false;
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class StreakInfoDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime? StreakStartDate { get; set; }
        public DateTime? StreakEndDate { get; set; }
        public List<DateTime> StreakDays { get; set; } = new();
    }

    public class ProductivityInsightsDto
    {
        public string BestTimeOfWeek { get; set; }
        public string MostProductiveCategory { get; set; }
        public decimal AverageTaskCompletionTime { get; set; }
        public int PeakProductivityHour { get; set; }
        public Dictionary<string, decimal> CategoryProductivity { get; set; } = new();
        public Dictionary<string, int> WeeklyPattern { get; set; } = new();
    }
}