namespace SphereScheduleAPI.Application.DTOs
{
    public class DashboardOverviewDto
    {
        public TaskStatsDto TaskStats { get; set; } = new();
        public AppointmentStatsDto AppointmentStats { get; set; } = new();
        public ProductivityStatsDto ProductivityStats { get; set; } = new();
        public UpcomingItemsDto UpcomingItems { get; set; } = new();
        public RecentActivityForDashboardDto RecentActivity { get; set; } = new();
        public UserStatsDto UserStats { get; set; } = new();
    }

    public class TaskStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TodayTasks { get; set; }
        public int UpcomingTasks { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> TasksByCategory { get; set; } = new();
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
    }

    public class AppointmentStatsDto
    {
        public int TotalAppointments { get; set; }
        public int ScheduledAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int VirtualAppointments { get; set; }
        public int InPersonAppointments { get; set; }
        public Dictionary<string, int> AppointmentsByType { get; set; } = new();
    }

    public class ProductivityStatsDto
    {
        public double AverageCompletionRate { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int TasksCompletedToday { get; set; }
        public int TasksCompletedThisWeek { get; set; }
        public int TasksCompletedThisMonth { get; set; }
        public Dictionary<string, double> WeeklyProductivity { get; set; } = new();
        public Dictionary<string, int> DailyCompletionTrend { get; set; } = new();
    }

    public class UpcomingItemsDto
    {
        public List<UpcomingTaskDto> UpcomingTasks { get; set; } = new();
        public List<UpcomingAppointmentDto> UpcomingAppointments { get; set; } = new();
        public List<UpcomingReminderDto> UpcomingReminders { get; set; } = new();
    }

    public class UpcomingTaskDto
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTimeOffset? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CompletionPercentage { get; set; }
        public string DueStatus { get; set; } = string.Empty; // today, tomorrow, overdue, upcoming
        public int DaysUntilDue { get; set; }
    }

    public class UpcomingAppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public string? Location { get; set; }
        public bool IsVirtual { get; set; }
        public string? MeetingLink { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public bool HasReminders { get; set; }
        public string TimeUntil { get; set; } = string.Empty; // in 2 hours, tomorrow, etc.
    }

    public class UpcomingReminderDto
    {
        public Guid ReminderId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ReminderType { get; set; } = string.Empty;
        public DateTimeOffset ReminderDateTime { get; set; }
        public Guid? TaskId { get; set; }
        public Guid? AppointmentId { get; set; }
        public string? TaskTitle { get; set; }
        public string? AppointmentTitle { get; set; }
        public string TimeUntil { get; set; } = string.Empty;
    }

    public class RecentActivityForDashboardDto
    {
        public List<RecentTaskActivityDto> RecentTasks { get; set; } = new();
        public List<RecentAppointmentActivityDto> RecentAppointments { get; set; } = new();
        public List<RecentUserActivityDto> RecentUserActivities { get; set; } = new();
        public int TotalActivitiesToday { get; set; }
    }

    public class RecentTaskActivityDto
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // created, completed, updated
        public DateTimeOffset Timestamp { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class RecentAppointmentActivityDto
    {
        public Guid AppointmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // created, completed, cancelled, updated
        public DateTimeOffset Timestamp { get; set; }
        public string AppointmentType { get; set; } = string.Empty;
    }

    public class RecentUserActivityDto
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalCategories { get; set; }
        public int TotalReminders { get; set; }
        public int ActiveReminders { get; set; }
        public int TotalParticipants { get; set; }
        public int DaysSinceRegistration { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public int AverageDailyTasks { get; set; }
        public int PeakProductivityHour { get; set; }
    }

    public class DashboardFilterDto
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? CategoryFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public string? StatusFilter { get; set; }
        public bool IncludeDetails { get; set; } = true;
        public int UpcomingDays { get; set; } = 7;
        public int RecentItemsCount { get; set; } = 10;
    }

    public class ProductivityReportDto
    {
        public DateTimeOffset PeriodStart { get; set; }
        public DateTimeOffset PeriodEnd { get; set; }
        public int TotalTasksCreated { get; set; }
        public int TotalTasksCompleted { get; set; }
        public double CompletionPercentage { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public double AppointmentCompletionRate { get; set; }
        public int TotalTimeSpentMinutes { get; set; }
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();
        public Dictionary<string, int> PriorityDistribution { get; set; } = new();
        public Dictionary<string, double> DailyProductivity { get; set; } = new();
        public List<string> TopPerformingDays { get; set; } = new();
        public string MostProductiveHour { get; set; } = string.Empty;
        public string MostCommonCategory { get; set; } = string.Empty;
        public string MostCommonPriority { get; set; } = string.Empty;
    }

    public class TimeUsageDto
    {
        public int WorkMinutes { get; set; }
        public int PersonalMinutes { get; set; }
        public int HealthMinutes { get; set; }
        public int OtherMinutes { get; set; }
        public int TotalTrackedMinutes { get; set; }
        public Dictionary<string, int> TimeByCategory { get; set; } = new();
        public Dictionary<string, double> PercentageByCategory { get; set; } = new();
        public double AverageTaskDuration { get; set; }
        public double AverageAppointmentDuration { get; set; }
    }

    public class NotificationSummaryDto
    {
        public int UnreadReminders { get; set; }
        public int PendingInvitations { get; set; }
        public int UpcomingDeadlines { get; set; }
        public int OverdueItems { get; set; }
        public List<NotificationItemDto> RecentNotifications { get; set; } = new();
    }

    public class NotificationItemDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // reminder, invitation, deadline, overdue
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsRead { get; set; }
        public Guid? EntityId { get; set; }
        public string? EntityType { get; set; }
    }
}