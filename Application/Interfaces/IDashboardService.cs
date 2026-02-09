using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid userId, DashboardFilterDto filterDto);
        Task<TaskStatsDto> GetTaskStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<AppointmentStatsDto> GetAppointmentStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<ProductivityStatsDto> GetProductivityStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<UpcomingItemsDto> GetUpcomingItemsAsync(Guid userId, int daysAhead = 7);
        Task<RecentActivityForDashboardDto> GetRecentActivityAsync(Guid userId, int count = 10);
        Task<UserStatsDto> GetUserStatisticsAsync(Guid userId);
        Task<ProductivityReportDto> GetProductivityReportAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<TimeUsageDto> GetTimeUsageStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(Guid userId);
        Task<Dictionary<string, object>> GetQuickStatsAsync(Guid userId);
        Task<IEnumerable<object>> GetCategoryBreakdownAsync(Guid userId);
        Task<IEnumerable<object>> GetPriorityBreakdownAsync(Guid userId);
        Task<IEnumerable<object>> GetMonthlyTrendAsync(Guid userId, int months = 6);
    }
}