using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid UserID, DashboardFilterDto filterDto);
        Task<TaskStatsDto> GetTaskStatisticsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<AppointmentStatsDto> GetAppointmentStatisticsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<ProductivityStatsDto> GetProductivityStatisticsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<UpcomingItemsDto> GetUpcomingItemsAsync(Guid UserID, int daysAhead = 7);
        Task<RecentActivityForDashboardDto> GetRecentActivityAsync(Guid UserID, int count = 10);
        Task<UserStatsDto> GetUserStatisticsAsync(Guid UserID);
        Task<ProductivityReportDto> GetProductivityReportAsync(Guid UserID, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<TimeUsageDto> GetTimeUsageStatisticsAsync(Guid UserID, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(Guid UserID);
        Task<Dictionary<string, object>> GetQuickStatsAsync(Guid UserID);
        Task<IEnumerable<object>> GetCategoryBreakdownAsync(Guid UserID);
        Task<IEnumerable<object>> GetPriorityBreakdownAsync(Guid UserID);
        Task<IEnumerable<object>> GetMonthlyTrendAsync(Guid UserID, int months = 6);
    }
}