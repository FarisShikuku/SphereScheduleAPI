using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;

namespace SphereScheduleAPI.Application.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ActivityLogService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ActivityLogDto> CreateActivityLogAsync(CreateActivityLogDto createDto)
        {
            try
            {
                // Validate user exists if UserId is provided
                if (createDto.UserId.HasValue)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.UserId == createDto.UserId.Value);
                    if (!userExists)
                    {
                        _logger.LogWarning("User with ID {UserId} not found when creating activity log", createDto.UserId);
                        // Continue anyway - we still want to log the activity
                    }
                }

                var activityLog = _mapper.Map<ActivityLog>(createDto);
                activityLog.CreatedAt = DateTimeOffset.UtcNow;

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Created activity log {LogId} for user {UserId}, activity: {ActivityType}", 
                    activityLog.LogId, createDto.UserId, createDto.ActivityType);

                return await GetActivityLogByIdAsync(activityLog.LogId, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating activity log for user {UserId}", createDto.UserId);
                throw;
            }
        }

        public async Task<ActivityLogDto> GetActivityLogByIdAsync(long logId, bool includeDetails = false)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (includeDetails)
            {
                query = query.Include(al => al.User);
            }

            var activityLog = await query.FirstOrDefaultAsync(al => al.LogId == logId);

            if (activityLog == null)
            {
                throw new KeyNotFoundException($"Activity log with ID {logId} not found");
            }

            var activityLogDto = _mapper.Map<ActivityLogDto>(activityLog);

            // Add additional details if requested and available
            if (includeDetails && activityLog.User != null)
            {
                activityLogDto.UserEmail = activityLog.User.Email;
                activityLogDto.UserDisplayName = activityLog.User.DisplayName;
            }

            // Try to get entity title if entity type and ID are available
            if (!string.IsNullOrEmpty(activityLog.EntityType) && activityLog.EntityId.HasValue)
            {
                activityLogDto.EntityTitle = await GetEntityTitleAsync(activityLog.EntityType, activityLog.EntityId.Value);
            }

            return activityLogDto;
        }

        public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsByFilterAsync(ActivityLogFilterDto filterDto)
        {
            var query = _context.ActivityLogs.AsQueryable();

            // Apply filters
            if (filterDto.UserId.HasValue)
            {
                query = query.Where(al => al.UserId == filterDto.UserId.Value);
            }

            if (!string.IsNullOrEmpty(filterDto.ActivityType))
            {
                query = query.Where(al => al.ActivityType == filterDto.ActivityType);
            }

            if (!string.IsNullOrEmpty(filterDto.EntityType))
            {
                query = query.Where(al => al.EntityType == filterDto.EntityType);
            }

            if (filterDto.EntityId.HasValue)
            {
                query = query.Where(al => al.EntityId == filterDto.EntityId.Value);
            }

            if (!string.IsNullOrEmpty(filterDto.Status))
            {
                query = query.Where(al => al.Status == filterDto.Status);
            }

            if (!string.IsNullOrEmpty(filterDto.IpAddress))
            {
                query = query.Where(al => al.IpAddress != null && al.IpAddress.Contains(filterDto.IpAddress));
            }

            if (filterDto.StartDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt >= filterDto.StartDate.Value);
            }

            if (filterDto.EndDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt <= filterDto.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(filterDto.SearchTerm))
            {
                query = query.Where(al => 
                    al.Details != null && al.Details.Contains(filterDto.SearchTerm) ||
                    al.ActivityType.Contains(filterDto.SearchTerm) ||
                    (al.EntityType != null && al.EntityType.Contains(filterDto.SearchTerm))
                );
            }

            // Include user details if requested
            if (filterDto.IncludeUserDetails.HasValue && filterDto.IncludeUserDetails.Value)
            {
                query = query.Include(al => al.User);
            }

            // Apply sorting
            query = ApplySorting(query, filterDto.SortBy, filterDto.SortDescending);

            // Apply pagination
            query = query
                .Skip((filterDto.PageNumber - 1) * filterDto.PageSize)
                .Take(filterDto.PageSize);

            var activityLogs = await query.ToListAsync();
            var activityLogDtos = _mapper.Map<IEnumerable<ActivityLogDto>>(activityLogs);

            // Add additional details if requested
            if (filterDto.IncludeUserDetails.HasValue && filterDto.IncludeUserDetails.Value)
            {
                foreach (var log in activityLogDtos)
                {
                    var activityLog = activityLogs.First(al => al.LogId == log.LogId);
                    if (activityLog.User != null)
                    {
                        log.UserEmail = activityLog.User.Email;
                        log.UserDisplayName = activityLog.User.DisplayName;
                    }

                    // Get entity titles if requested
                    if (filterDto.IncludeEntityDetails.HasValue && filterDto.IncludeEntityDetails.Value &&
                        !string.IsNullOrEmpty(activityLog.EntityType) && activityLog.EntityId.HasValue)
                    {
                        log.EntityTitle = await GetEntityTitleAsync(activityLog.EntityType, activityLog.EntityId.Value);
                    }
                }
            }

            return activityLogDtos;
        }

        public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsByUserIdAsync(Guid userId, ActivityLogFilterDto filterDto)
        {
            filterDto.UserId = userId;
            return await GetActivityLogsByFilterAsync(filterDto);
        }

        public async Task<IEnumerable<ActivityLogDto>> GetRecentActivitiesAsync(int count = 20)
        {
            var activities = await _context.ActivityLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.CreatedAt)
                .Take(count)
                .ToListAsync();

            var activityDtos = _mapper.Map<IEnumerable<ActivityLogDto>>(activities);

            // Add user details
            foreach (var log in activityDtos)
            {
                var activity = activities.First(al => al.LogId == log.LogId);
                if (activity.User != null)
                {
                    log.UserEmail = activity.User.Email;
                    log.UserDisplayName = activity.User.DisplayName;
                }
            }

            return activityDtos;
        }

        public async Task<ActivityStatisticsDto> GetActivityStatisticsAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt <= endDate.Value);
            }

            var activities = await query.ToListAsync();
            var recentActivities = await GetRecentActivitiesAsync(10);

            var statistics = new ActivityStatisticsDto
            {
                TotalActivities = activities.Count,
                SuccessfulActivities = activities.Count(al => al.Status == "success"),
                FailedActivities = activities.Count(al => al.Status == "error"),
                WarningActivities = activities.Count(al => al.Status == "warning"),
                RecentActivities = recentActivities.Select(ra => new RecentActivityDto
                {
                    LogId = ra.LogId,
                    ActivityType = ra.ActivityType,
                    EntityType = ra.EntityType,
                    Details = ra.Details,
                    CreatedAt = ra.CreatedAt,
                    UserDisplayName = ra.UserDisplayName
                }).ToList()
            };

            // Group by activity type
            statistics.ActivitiesByType = activities
                .GroupBy(al => al.ActivityType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by entity type (excluding null)
            statistics.ActivitiesByEntity = activities
                .Where(al => al.EntityType != null)
                .GroupBy(al => al.EntityType!)
                .ToDictionary(g => g.Key, g => g.Count());

            // Daily activity count (last 30 days)
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
            var dailyActivities = activities
                .Where(al => al.CreatedAt >= thirtyDaysAgo)
                .GroupBy(al => al.CreatedAt.Date.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            statistics.DailyActivityCount = dailyActivities;

            // User activity count (top 10 users)
            var userActivities = activities
                .Where(al => al.UserId.HasValue)
                .GroupBy(al => al.UserId!.Value)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            statistics.UserActivityCount = userActivities;

            return statistics;
        }

        public async Task<UserActivitySummaryDto> GetUserActivitySummaryAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            var userActivities = await _context.ActivityLogs
                .Where(al => al.UserId == userId)
                .ToListAsync();

            var summary = new UserActivitySummaryDto
            {
                UserId = userId,
                UserEmail = user.Email,
                UserDisplayName = user.DisplayName ?? user.Email,
                TotalActivities = userActivities.Count,
                FirstActivity = userActivities.Any() ? userActivities.Min(al => al.CreatedAt) : null,
                LastActivity = userActivities.Any() ? userActivities.Max(al => al.CreatedAt) : null
            };

            // Group activities by type
            summary.ActivityTypes = userActivities
                .GroupBy(al => al.ActivityType)
                .ToDictionary(g => g.Key, g => g.Count());

            return summary;
        }

        public async Task<AuditTrailDto> GetAuditTrailAsync(string entityType, Guid entityId)
        {
            var activities = await _context.ActivityLogs
                .Include(al => al.User)
                .Where(al => al.EntityType == entityType && al.EntityId == entityId)
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();

            var activityDtos = _mapper.Map<List<ActivityLogDto>>(activities);

            // Add user details
            foreach (var log in activityDtos)
            {
                var activity = activities.First(al => al.LogId == log.LogId);
                if (activity.User != null)
                {
                    log.UserEmail = activity.User.Email;
                    log.UserDisplayName = activity.User.DisplayName;
                }
            }

            return new AuditTrailDto
            {
                EntityType = entityType,
                EntityId = entityId,
                Activities = activityDtos
            };
        }

        public async Task<IEnumerable<ActivityLogDto>> GetFailedActivitiesAsync(DateTimeOffset? since = null)
        {
            var query = _context.ActivityLogs
                .Include(al => al.User)
                .Where(al => al.Status == "error");

            if (since.HasValue)
            {
                query = query.Where(al => al.CreatedAt >= since.Value);
            }

            var activities = await query
                .OrderByDescending(al => al.CreatedAt)
                .Take(100)
                .ToListAsync();

            var activityDtos = _mapper.Map<IEnumerable<ActivityLogDto>>(activities);

            // Add user details
            foreach (var log in activityDtos)
            {
                var activity = activities.First(al => al.LogId == log.LogId);
                if (activity.User != null)
                {
                    log.UserEmail = activity.User.Email;
                    log.UserDisplayName = activity.User.DisplayName;
                }
            }

            return activityDtos;
        }

        public async Task<int> CleanupOldLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);
            
            var oldLogs = await _context.ActivityLogs
                .Where(al => al.CreatedAt < cutoffDate)
                .ToListAsync();

            if (!oldLogs.Any())
            {
                return 0;
            }

            _context.ActivityLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} activity logs older than {CutoffDate}", 
                oldLogs.Count, cutoffDate);
            
            return oldLogs.Count;
        }

        public async Task LogUserLoginAsync(Guid userId, string ipAddress, string userAgent, bool success, string? details = null)
        {
            var logDto = new CreateActivityLogDto
            {
                UserId = userId,
                ActivityType = "login",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Status = success ? "success" : "error",
                Details = details ?? (success ? "User logged in successfully" : "Failed login attempt")
            };

            await CreateActivityLogAsync(logDto);
        }

        public async Task LogUserLogoutAsync(Guid userId, string ipAddress, string userAgent)
        {
            var logDto = new CreateActivityLogDto
            {
                UserId = userId,
                ActivityType = "logout",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Status = "success",
                Details = "User logged out"
            };

            await CreateActivityLogAsync(logDto);
        }

        public async Task LogEntityCreatedAsync(string entityType, Guid entityId, Guid userId, string details)
        {
            var logDto = new CreateActivityLogDto
            {
                UserId = userId,
                ActivityType = $"create_{entityType.ToLower()}",
                EntityType = entityType,
                EntityId = entityId,
                Status = "success",
                Details = details
            };

            await CreateActivityLogAsync(logDto);
        }

        public async Task LogEntityUpdatedAsync(string entityType, Guid entityId, Guid userId, string details)
        {
            var logDto = new CreateActivityLogDto
            {
                UserId = userId,
                ActivityType = $"update_{entityType.ToLower()}",
                EntityType = entityType,
                EntityId = entityId,
                Status = "success",
                Details = details
            };

            await CreateActivityLogAsync(logDto);
        }

        public async Task LogEntityDeletedAsync(string entityType, Guid entityId, Guid userId, string details)
        {
            var logDto = new CreateActivityLogDto
            {
                UserId = userId,
                ActivityType = $"delete_{entityType.ToLower()}",
                EntityType = entityType,
                EntityId = entityId,
                Status = "success",
                Details = details
            };

            await CreateActivityLogAsync(logDto);
        }

        public async Task LogErrorAsync(string activityType, Guid? userId, string details, string? entityType = null, Guid? entityId = null)
        {
            var logDto = new CreateActivityLogDto
            {
                UserId = userId,
                ActivityType = activityType,
                EntityType = entityType,
                EntityId = entityId,
                Status = "error",
                Details = details
            };

            await CreateActivityLogAsync(logDto);
        }

        private IQueryable<ActivityLog> ApplySorting(IQueryable<ActivityLog> query, string? sortBy, bool sortDescending)
        {
            return (sortBy?.ToLower(), sortDescending) switch
            {
                ("logid", false) => query.OrderBy(al => al.LogId),
                ("logid", true) => query.OrderByDescending(al => al.LogId),
                ("activitytype", false) => query.OrderBy(al => al.ActivityType),
                ("activitytype", true) => query.OrderByDescending(al => al.ActivityType),
                ("entitytype", false) => query.OrderBy(al => al.EntityType),
                ("entitytype", true) => query.OrderByDescending(al => al.EntityType),
                ("status", false) => query.OrderBy(al => al.Status),
                ("status", true) => query.OrderByDescending(al => al.Status),
                ("createdat", false) => query.OrderBy(al => al.CreatedAt),
                ("createdat", true) => query.OrderByDescending(al => al.CreatedAt),
                _ => query.OrderByDescending(al => al.CreatedAt)
            };
        }

        private async Task<string?> GetEntityTitleAsync(string entityType, Guid entityId)
        {
            try
            {
                return entityType.ToLower() switch
                {
                    "user" => await _context.Users
                        .Where(u => u.UserId == entityId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync(),
                    
                    "task" => await _context.Tasks
                        .Where(t => t.TaskId == entityId)
                        .Select(t => t.Title)
                        .FirstOrDefaultAsync(),
                    
                    "appointment" => await _context.Appointments
                        .Where(a => a.AppointmentId == entityId)
                        .Select(a => a.Title)
                        .FirstOrDefaultAsync(),
                    
                    "category" => await _context.Categories
                        .Where(c => c.CategoryId == entityId)
                        .Select(c => c.CategoryName)
                        .FirstOrDefaultAsync(),
                    
                    "reminder" => await _context.Reminders
                        .Where(r => r.ReminderId == entityId)
                        .Select(r => r.Title)
                        .FirstOrDefaultAsync(),
                    
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity title for {EntityType} with ID {EntityId}", entityType, entityId);
                return null;
            }
        }
    }
}