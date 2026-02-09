using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class ActivityLogDto
    {
        public long LogId { get; set; }
        public Guid? UserId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
        public string Status { get; set; } = "success";
        public DateTimeOffset CreatedAt { get; set; }

        // Additional info
        public string? UserEmail { get; set; }
        public string? UserDisplayName { get; set; }
        public string? EntityTitle { get; set; }
    }

    public class CreateActivityLogDto
    {
        [Required]
        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [RegularExpression("^(login|logout|create_task|update_task|delete_task|create_appointment|update_appointment|delete_appointment|share_item|export_data|change_settings)$",
            ErrorMessage = "Invalid activity type")]
        public string ActivityType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EntityType { get; set; }

        public Guid? EntityId { get; set; }

        [MaxLength(45)]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            ErrorMessage = "Invalid IP address format")]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(4000)]
        public string? Details { get; set; }

        [MaxLength(20)]
        [RegularExpression("^(success|error|warning)$", ErrorMessage = "Status must be 'success', 'error', or 'warning'")]
        public string Status { get; set; } = "success";
    }

    public class ActivityLogFilterDto
    {
        public Guid? UserId { get; set; }
        public string? ActivityType { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? Status { get; set; }
        public string? IpAddress { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IncludeUserDetails { get; set; } = false;
        public bool? IncludeEntityDetails { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }

    public class ActivityStatisticsDto
    {
        public int TotalActivities { get; set; }
        public int SuccessfulActivities { get; set; }
        public int FailedActivities { get; set; }
        public int WarningActivities { get; set; }
        public Dictionary<string, int> ActivitiesByType { get; set; } = new();
        public Dictionary<string, int> ActivitiesByEntity { get; set; } = new();
        public Dictionary<string, int> DailyActivityCount { get; set; } = new();
        public Dictionary<string, int> UserActivityCount { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public long LogId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? Details { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? UserDisplayName { get; set; }
    }

    public class UserActivitySummaryDto
    {
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public DateTimeOffset? FirstActivity { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public Dictionary<string, int> ActivityTypes { get; set; } = new();
    }

    public class AuditTrailDto
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public List<ActivityLogDto> Activities { get; set; } = new();
    }
}