using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IActivityLogService
    {
        Task<ActivityLogDto> CreateActivityLogAsync(CreateActivityLogDto createDto);
        Task<ActivityLogDto> GetActivityLogByIdAsync(long logId, bool includeDetails = false);
        Task<IEnumerable<ActivityLogDto>> GetActivityLogsByFilterAsync(ActivityLogFilterDto filterDto);
        Task<IEnumerable<ActivityLogDto>> GetActivityLogsByUserIDAsync(Guid UserID, ActivityLogFilterDto filterDto);
        Task<IEnumerable<ActivityLogDto>> GetRecentActivitiesAsync(int count = 20);
        Task<ActivityStatisticsDto> GetActivityStatisticsAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        Task<UserActivitySummaryDto> GetUserActivitySummaryAsync(Guid UserID);
        Task<AuditTrailDto> GetAuditTrailAsync(string entityType, Guid entityId);
        Task<IEnumerable<ActivityLogDto>> GetFailedActivitiesAsync(DateTimeOffset? since = null);
        Task<int> CleanupOldLogsAsync(int daysToKeep = 90);
        Task LogUserLoginAsync(Guid UserID, string ipAddress, string userAgent, bool success, string? details = null);
        Task LogUserLogoutAsync(Guid UserID, string ipAddress, string userAgent);
        Task LogEntityCreatedAsync(string entityType, Guid entityId, Guid UserID, string details);
        Task LogEntityUpdatedAsync(string entityType, Guid entityId, Guid UserID, string details);
        Task LogEntityDeletedAsync(string entityType, Guid entityId, Guid UserID, string details);
        Task LogErrorAsync(string activityType, Guid? UserID, string details, string? entityType = null, Guid? entityId = null);
    }
}