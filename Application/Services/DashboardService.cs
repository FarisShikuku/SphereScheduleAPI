using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;

namespace SphereScheduleAPI.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid userId, DashboardFilterDto filterDto)
        {
            try
            {
                var overview = new DashboardOverviewDto();

                // Get all statistics in parallel for better performance
                var taskStatsTask = GetTaskStatisticsAsync(userId, filterDto.StartDate, filterDto.EndDate);
                var appointmentStatsTask = GetAppointmentStatisticsAsync(userId, filterDto.StartDate, filterDto.EndDate);
                var productivityStatsTask = GetProductivityStatisticsAsync(userId, filterDto.StartDate, filterDto.EndDate);
                var upcomingItemsTask = GetUpcomingItemsAsync(userId, filterDto.UpcomingDays);
                var recentActivityTask = GetRecentActivityAsync(userId, filterDto.RecentItemsCount);
                var userStatsTask = GetUserStatisticsAsync(userId);

                await Task.WhenAll(
                    taskStatsTask,
                    appointmentStatsTask,
                    productivityStatsTask,
                    upcomingItemsTask,
                    recentActivityTask,
                    userStatsTask
                );

                overview.TaskStats = await taskStatsTask;
                overview.AppointmentStats = await appointmentStatsTask;
                overview.ProductivityStats = await productivityStatsTask;
                overview.UpcomingItems = await upcomingItemsTask;
                overview.RecentActivity = await recentActivityTask;
                overview.UserStats = await userStatsTask;

                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview for user {UserId}", userId);
                throw;
            }
        }

        public async Task<TaskStatsDto> GetTaskStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted);

            if (startDate != null)
            {
                query = query.Where(t => t.CreatedAt >= startDate);
            }

            if (endDate != null)
            {
                query = query.Where(t => t.CreatedAt <= endDate);
            }

            var tasks = await query.ToListAsync();
            var today = DateTime.UtcNow.Date; // Changed to DateTime.UtcNow for Date comparison

            var stats = new TaskStatsDto
            {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status == "completed"),
                InProgressTasks = tasks.Count(t => t.Status == "in_progress"),
                PendingTasks = tasks.Count(t => t.Status == "pending"),
                OverdueTasks = tasks.Count(t => t.Status != "completed" && t.DueDate.HasValue && t.DueDate.Value.Date < today),
                TodayTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == today && t.Status != "completed"),
                UpcomingTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date > today && t.Status != "completed"),
                CompletionRate = tasks.Any() ? (tasks.Count(t => t.Status == "completed") * 100.0) / tasks.Count : 0
            };

            // Group by category
            stats.TasksByCategory = tasks
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by priority
            stats.TasksByPriority = tasks
                .GroupBy(t => t.PriorityLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        public async Task<AppointmentStatsDto> GetAppointmentStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var query = _context.Appointments
                .Where(a => a.UserId == userId && !a.IsDeleted);

            if (startDate != null)
            {
                query = query.Where(a => a.StartDateTime >= startDate);
            }

            if (endDate != null)
            {
                query = query.Where(a => a.StartDateTime <= endDate);
            }

            var appointments = await query.ToListAsync();
            var today = DateTimeOffset.UtcNow.Date;

            var stats = new AppointmentStatsDto
            {
                TotalAppointments = appointments.Count,
                ScheduledAppointments = appointments.Count(a => a.Status == "scheduled"),
                CompletedAppointments = appointments.Count(a => a.Status == "completed"),
                CancelledAppointments = appointments.Count(a => a.Status == "cancelled"),
                ConfirmedAppointments = appointments.Count(a => a.Status == "confirmed"),
                TodayAppointments = appointments.Count(a => a.StartDateTime.Date == today && a.Status == "scheduled"),
                UpcomingAppointments = appointments.Count(a => a.StartDateTime.Date > today && a.Status == "scheduled"),
                VirtualAppointments = appointments.Count(a => a.IsVirtual),
                InPersonAppointments = appointments.Count(a => !a.IsVirtual)
            };

            // Group by type
            stats.AppointmentsByType = appointments
                .GroupBy(a => a.AppointmentType)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        public async Task<ProductivityStatsDto> GetProductivityStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var today = DateTime.UtcNow.Date; // Changed to DateTime
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Get tasks for date range
            var taskQuery = _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted);

            if (startDate != null)
            {
                taskQuery = taskQuery.Where(t => t.CreatedAt >= startDate);
            }

            if (endDate != null)
            {
                taskQuery = taskQuery.Where(t => t.CreatedAt <= endDate);
            }

            var tasks = await taskQuery.ToListAsync();

            var stats = new ProductivityStatsDto
            {
                TasksCompletedToday = tasks.Count(t => t.Status == "completed" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date == DateTime.UtcNow.Date),
                TasksCompletedThisWeek = tasks.Count(t => t.Status == "completed" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= weekStart),
                TasksCompletedThisMonth = tasks.Count(t => t.Status == "completed" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= monthStart),
                AverageCompletionRate = tasks.Any() ? tasks.Average(t => t.CompletionPercentage) : 0
            };

            // Calculate streak
            stats.CurrentStreak = await CalculateCurrentStreakAsync(userId);
            stats.BestStreak = await CalculateBestStreakAsync(userId);

            // Weekly productivity (last 4 weeks)
            for (int i = 3; i >= 0; i--)
            {
                var weekStartDate = today.AddDays(-(i * 7 + (int)today.DayOfWeek));
                var weekEndDate = weekStartDate.AddDays(6);

                var weekTasks = tasks.Where(t =>
                    t.CompletedAt.HasValue &&
                    t.CompletedAt.Value.Date >= weekStartDate &&
                    t.CompletedAt.Value.Date <= weekEndDate
                ).ToList();

                var weekProductivity = weekTasks.Any() ?
                    (weekTasks.Count * 100.0) / tasks.Count(t =>
                        t.CreatedAt.Date >= weekStartDate &&
                        t.CreatedAt.Date <= weekEndDate) : 0;

                stats.WeeklyProductivity[$"Week {4 - i}"] = weekProductivity;
            }

            // Daily completion trend (last 7 days)
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dailyCompleted = tasks.Count(t =>
                    t.Status == "completed" &&
                    t.CompletedAt.HasValue &&
                    t.CompletedAt.Value.Date == date);

                stats.DailyCompletionTrend[date.ToString("MMM dd")] = dailyCompleted;
            }

            return stats;
        }

        public async Task<UpcomingItemsDto> GetUpcomingItemsAsync(Guid userId, int daysAhead = 7)
        {
            var today = DateTime.UtcNow.Date; // Changed to DateTime
            var endDate = today.AddDays(daysAhead);

            var upcomingItems = new UpcomingItemsDto();

            // Get upcoming tasks
            var upcomingTasks = await _context.Tasks
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.DueDate.HasValue &&
                           t.DueDate.Value.Date >= today &&
                           t.DueDate.Value.Date <= endDate &&
                           t.Status != "completed")
                .OrderBy(t => t.DueDate)
                .Take(20)
                .ToListAsync();

            upcomingItems.UpcomingTasks = upcomingTasks.Select(t => new UpcomingTaskDto
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Category = t.Category,
                Priority = t.PriorityLevel,
                DueDate = t.DueDate,
                DueTime = t.DueTime,
                Status = t.Status,
                CompletionPercentage = t.CompletionPercentage,
                DueStatus = GetDueStatus(t.DueDate),
                DaysUntilDue = t.DueDate.HasValue ? (t.DueDate.Value.Date - today).Days : 0
            }).ToList();

            // Get upcoming appointments
            var upcomingAppointments = await _context.Appointments
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.StartDateTime >= DateTimeOffset.UtcNow &&
                           a.StartDateTime <= DateTimeOffset.UtcNow.AddDays(daysAhead) &&
                           a.Status == "scheduled")
                .OrderBy(a => a.StartDateTime)
                .Take(15)
                .ToListAsync();

            upcomingItems.UpcomingAppointments = upcomingAppointments.Select(a => new UpcomingAppointmentDto
            {
                AppointmentId = a.AppointmentId,
                Title = a.Title,
                AppointmentType = a.AppointmentType,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                Location = a.Location,
                IsVirtual = a.IsVirtual,
                MeetingLink = a.MeetingLink,
                Status = a.Status,
                ParticipantCount = _context.Participants.Count(p => p.AppointmentId == a.AppointmentId),
                HasReminders = _context.Reminders.Any(r => r.AppointmentId == a.AppointmentId && r.Status == "pending"),
                TimeUntil = GetTimeUntil(a.StartDateTime)
            }).ToList();

            // Get upcoming reminders
            var upcomingReminders = await _context.Reminders
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .Where(r => r.UserId == userId &&
                           r.Status == "pending" &&
                           r.ReminderDateTime >= DateTimeOffset.UtcNow &&
                           r.ReminderDateTime <= DateTimeOffset.UtcNow.AddDays(daysAhead))
                .OrderBy(r => r.ReminderDateTime)
                .Take(15)
                .ToListAsync();

            upcomingItems.UpcomingReminders = upcomingReminders.Select(r => new UpcomingReminderDto
            {
                ReminderId = r.ReminderId,
                Title = r.Title,
                ReminderType = r.ReminderType,
                ReminderDateTime = r.ReminderDateTime,
                TaskId = r.TaskId,
                AppointmentId = r.AppointmentId,
                TaskTitle = r.Task?.Title,
                AppointmentTitle = r.Appointment?.Title,
                TimeUntil = GetTimeUntil(r.ReminderDateTime)
            }).ToList();

            return upcomingItems;
        }

        public async Task<RecentActivityForDashboardDto> GetRecentActivityAsync(Guid userId, int count = 10)
        {
            var recentActivity = new RecentActivityForDashboardDto();
            var today = DateTimeOffset.UtcNow.Date;

            // Get recent task activities
            var recentTasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.UpdatedAt)
                .Take(count)
                .ToListAsync();

            recentActivity.RecentTasks = recentTasks.Select(t => new RecentTaskActivityDto
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Action = GetTaskAction(t),
                Timestamp = t.UpdatedAt,
                Category = t.Category
            }).ToList();

            // Get recent appointment activities
            var recentAppointments = await _context.Appointments
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .OrderByDescending(a => a.UpdatedAt)
                .Take(count)
                .ToListAsync();

            recentActivity.RecentAppointments = recentAppointments.Select(a => new RecentAppointmentActivityDto
            {
                AppointmentId = a.AppointmentId,
                Title = a.Title,
                Action = GetAppointmentAction(a),
                Timestamp = a.UpdatedAt,
                AppointmentType = a.AppointmentType
            }).ToList();

            // Get recent user activities from activity logs
            var recentUserActivities = await _context.ActivityLogs
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.CreatedAt)
                .Take(count)
                .ToListAsync();

            recentActivity.RecentUserActivities = recentUserActivities.Select(al => new RecentUserActivityDto
            {
                ActivityType = al.ActivityType,
                Description = al.Details ?? al.ActivityType,
                Timestamp = al.CreatedAt,
                EntityType = al.EntityType,
                EntityId = al.EntityId
            }).ToList();

            // Count total activities today
            recentActivity.TotalActivitiesToday = await _context.ActivityLogs
                .CountAsync(al => al.UserId == userId && al.CreatedAt.Date == today);

            return recentActivity;
        }

        public async Task<UserStatsDto> GetUserStatisticsAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            var stats = new UserStatsDto
            {
                TotalCategories = await _context.Categories.CountAsync(c => c.UserId == userId && !c.IsDeleted),
                TotalReminders = await _context.Reminders.CountAsync(r => r.UserId == userId),
                ActiveReminders = await _context.Reminders.CountAsync(r => r.UserId == userId && r.Status == "pending"),
                TotalParticipants = await _context.Participants.CountAsync(p => p.Appointment.UserId == userId),
                DaysSinceRegistration = (int)(DateTimeOffset.UtcNow - user.CreatedAt).TotalDays, // Fixed: CreatedAt is non-nullable
                LastLogin = user.LastLoginAt,
                LastActivity = user.LastActivityAt
            };

            // Calculate average daily tasks
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .ToListAsync();

            if (tasks.Any())
            {
                var daysActive = Math.Max(1, (DateTimeOffset.UtcNow - user.CreatedAt).Days);
                stats.AverageDailyTasks = tasks.Count / daysActive;
            }

            // Find peak productivity hour
            var completedTasks = tasks.Where(t => t.CompletedAt.HasValue).ToList();
            if (completedTasks.Any())
            {
                var hourGroups = completedTasks
                    .GroupBy(t => t.CompletedAt!.Value.Hour)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                stats.PeakProductivityHour = hourGroups?.Key ?? 0;
            }

            return stats;
        }

        public async Task<ProductivityReportDto> GetProductivityReportAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var report = new ProductivityReportDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate
            };

            // Get tasks for the period
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate)
                .ToListAsync();

            var completedTasks = tasks.Where(t => t.Status == "completed").ToList();

            report.TotalTasksCreated = tasks.Count;
            report.TotalTasksCompleted = completedTasks.Count;
            report.CompletionPercentage = tasks.Any() ? (completedTasks.Count * 100.0) / tasks.Count : 0;

            // Get appointments for the period
            var appointments = await _context.Appointments
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.StartDateTime >= startDate &&
                           a.StartDateTime <= endDate)
                .ToListAsync();

            report.TotalAppointments = appointments.Count;
            report.CompletedAppointments = appointments.Count(a => a.Status == "completed");
            report.AppointmentCompletionRate = appointments.Any() ?
                (appointments.Count(a => a.Status == "completed") * 100.0) / appointments.Count : 0;

            // Calculate total time spent
            report.TotalTimeSpentMinutes = completedTasks.Sum(t => t.ActualDurationMinutes ?? 0) +
                                          appointments.Where(a => a.Status == "completed")
                                                     .Sum(a => (int)(a.EndDateTime - a.StartDateTime).TotalMinutes);

            // Category distribution
            report.CategoryDistribution = tasks
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Priority distribution
            report.PriorityDistribution = tasks
                .GroupBy(t => t.PriorityLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            // Daily productivity
            var days = Enumerable.Range(0, (int)(endDate - startDate).TotalDays + 1)
                .Select(offset => startDate.AddDays(offset).Date);

            foreach (var day in days)
            {
                var dayTasks = tasks.Where(t => t.CreatedAt.Date == day).ToList();
                var dayCompleted = dayTasks.Count(t => t.Status == "completed");
                var productivity = dayTasks.Any() ? (dayCompleted * 100.0) / dayTasks.Count : 0;

                report.DailyProductivity[day.ToString("MMM dd")] = productivity;
            }

            // Top performing days
            report.TopPerformingDays = report.DailyProductivity
                .OrderByDescending(kv => kv.Value)
                .Take(3)
                .Select(kv => kv.Key)
                .ToList();

            // Most productive hour
            var hourProductivity = completedTasks
                .Where(t => t.CompletedAt.HasValue)
                .GroupBy(t => t.CompletedAt!.Value.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            report.MostProductiveHour = hourProductivity != null ?
                $"{hourProductivity.Key}:00 - {hourProductivity.Key + 1}:00" : "N/A";

            // Most common category
            report.MostCommonCategory = report.CategoryDistribution
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault().Key ?? "N/A";

            // Most common priority
            report.MostCommonPriority = report.PriorityDistribution
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault().Key ?? "N/A";

            return report;
        }

        public async Task<TimeUsageDto> GetTimeUsageStatisticsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var timeUsage = new TimeUsageDto();

            // Get completed tasks with duration
            var completedTasks = await _context.Tasks
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Status == "completed" &&
                           t.ActualDurationMinutes.HasValue)
                .ToListAsync();

            // Get completed appointments with duration
            var completedAppointments = await _context.Appointments
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.Status == "completed")
                .ToListAsync();

            // Calculate time by category
            foreach (var task in completedTasks)
            {
                var duration = task.ActualDurationMinutes ?? 0;

                switch (task.Category.ToLower())
                {
                    case "work":
                    case "job":
                        timeUsage.WorkMinutes += duration;
                        break;
                    case "personal":
                        timeUsage.PersonalMinutes += duration;
                        break;
                    case "health":
                        timeUsage.HealthMinutes += duration;
                        break;
                    default:
                        timeUsage.OtherMinutes += duration;
                        break;
                }

                // Add to category dictionary
                if (!timeUsage.TimeByCategory.ContainsKey(task.Category))
                {
                    timeUsage.TimeByCategory[task.Category] = 0;
                }
                timeUsage.TimeByCategory[task.Category] += duration;
            }

            // Add appointment time (mostly work/business)
            var appointmentMinutes = completedAppointments
                .Sum(a => (int)(a.EndDateTime - a.StartDateTime).TotalMinutes);

            timeUsage.WorkMinutes += appointmentMinutes;
            if (appointmentMinutes > 0)
            {
                timeUsage.TimeByCategory["Appointments"] = appointmentMinutes;
            }

            // Calculate totals and percentages
            timeUsage.TotalTrackedMinutes = timeUsage.WorkMinutes + timeUsage.PersonalMinutes +
                                           timeUsage.HealthMinutes + timeUsage.OtherMinutes;

            if (timeUsage.TotalTrackedMinutes > 0)
            {
                timeUsage.PercentageByCategory["Work"] = (timeUsage.WorkMinutes * 100.0) / timeUsage.TotalTrackedMinutes;
                timeUsage.PercentageByCategory["Personal"] = (timeUsage.PersonalMinutes * 100.0) / timeUsage.TotalTrackedMinutes;
                timeUsage.PercentageByCategory["Health"] = (timeUsage.HealthMinutes * 100.0) / timeUsage.TotalTrackedMinutes;
                timeUsage.PercentageByCategory["Other"] = (timeUsage.OtherMinutes * 100.0) / timeUsage.TotalTrackedMinutes;
            }

            // Calculate averages
            timeUsage.AverageTaskDuration = completedTasks.Any() ?
                completedTasks.Average(t => t.ActualDurationMinutes ?? 0) : 0;

            timeUsage.AverageAppointmentDuration = completedAppointments.Any() ?
                completedAppointments.Average(a => (a.EndDateTime - a.StartDateTime).TotalMinutes) : 0;

            return timeUsage;
        }

        public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(Guid userId)
        {
            var summary = new NotificationSummaryDto();

            // Get unread/pending reminders
            summary.UnreadReminders = await _context.Reminders
                .CountAsync(r => r.UserId == userId && r.Status == "pending");

            // Get user email for invitation checks
            var userEmail = await GetUserEmailAsync(userId);

            // Get pending invitations (participants with pending status)
            summary.PendingInvitations = await _context.Participants
                .CountAsync(p => (p.UserId == userId || p.Email == userEmail) &&
                                p.InvitationStatus == "pending");

            // Get upcoming deadlines (tasks due today or tomorrow)
            var today = DateTime.UtcNow.Date; // Changed to DateTime
            summary.UpcomingDeadlines = await _context.Tasks
                .CountAsync(t => t.UserId == userId &&
                                !t.IsDeleted &&
                                t.Status != "completed" &&
                                t.DueDate.HasValue &&
                                t.DueDate.Value.Date >= today &&
                                t.DueDate.Value.Date <= today.AddDays(1));

            // Get overdue items
            summary.OverdueItems = await _context.Tasks
                .CountAsync(t => t.UserId == userId &&
                                !t.IsDeleted &&
                                t.Status != "completed" &&
                                t.DueDate.HasValue &&
                                t.DueDate.Value.Date < today);

            // Get recent notifications
            var recentNotifications = new List<NotificationItemDto>();

            // Add recent reminders
            var recentReminders = await _context.Reminders
                .Include(r => r.Task)
                .Include(r => r.Appointment)
                .Where(r => r.UserId == userId && r.Status == "pending")
                .OrderByDescending(r => r.ReminderDateTime)
                .Take(5)
                .ToListAsync();

            recentNotifications.AddRange(recentReminders.Select(r => new NotificationItemDto
            {
                Id = r.ReminderId,
                Type = "reminder",
                Title = r.Title,
                Message = $"Reminder: {r.Message}",
                Timestamp = r.ReminderDateTime,
                IsRead = false,
                EntityId = r.TaskId ?? r.AppointmentId,
                EntityType = r.TaskId.HasValue ? "task" : "appointment"
            }));

            // Add pending invitations
            var pendingInvites = await _context.Participants
                .Include(p => p.Appointment)
                .Where(p => (p.UserId == userId || p.Email == userEmail) &&
                           p.InvitationStatus == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            recentNotifications.AddRange(pendingInvites.Select(p => new NotificationItemDto
            {
                Id = p.ParticipantId,
                Type = "invitation",
                Title = $"Invitation: {p.Appointment?.Title}",
                Message = $"You've been invited to {p.Appointment?.Title}",
                Timestamp = p.CreatedAt,
                IsRead = false,
                EntityId = p.AppointmentId,
                EntityType = "appointment"
            }));

            summary.RecentNotifications = recentNotifications
                .OrderByDescending(n => n.Timestamp)
                .Take(10)
                .ToList();

            return summary;
        }

        public async Task<Dictionary<string, object>> GetQuickStatsAsync(Guid userId)
        {
            var today = DateTime.UtcNow.Date; // Changed to DateTime

            var quickStats = new Dictionary<string, object>();

            // Task quick stats
            quickStats["totalTasks"] = await _context.Tasks.CountAsync(t => t.UserId == userId && !t.IsDeleted);
            quickStats["completedTasksToday"] = await _context.Tasks.CountAsync(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                t.Status == "completed" &&
                t.CompletedAt.HasValue &&
                t.CompletedAt.Value.Date == DateTime.UtcNow.Date);

            quickStats["pendingTasks"] = await _context.Tasks.CountAsync(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                t.Status == "pending");

            quickStats["overdueTasks"] = await _context.Tasks.CountAsync(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                t.Status != "completed" &&
                t.DueDate.HasValue &&
                t.DueDate.Value.Date < today);

            // Appointment quick stats
            quickStats["todayAppointments"] = await _context.Appointments.CountAsync(a =>
                a.UserId == userId &&
                !a.IsDeleted &&
                a.StartDateTime.Date == DateTimeOffset.UtcNow.Date &&
                a.Status == "scheduled");

            quickStats["upcomingAppointments"] = await _context.Appointments.CountAsync(a =>
                a.UserId == userId &&
                !a.IsDeleted &&
                a.StartDateTime > DateTimeOffset.UtcNow &&
                a.Status == "scheduled");

            // Reminder quick stats
            quickStats["activeReminders"] = await _context.Reminders.CountAsync(r =>
                r.UserId == userId &&
                r.Status == "pending");

            // Category quick stats
            quickStats["totalCategories"] = await _context.Categories.CountAsync(c =>
                c.UserId == userId &&
                !c.IsDeleted);

            return quickStats;
        }

        public async Task<IEnumerable<object>> GetCategoryBreakdownAsync(Guid userId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .ToListAsync();

            var breakdown = tasks
                .GroupBy(t => t.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalTasks = g.Count(),
                    CompletedTasks = g.Count(t => t.Status == "completed"),
                    InProgressTasks = g.Count(t => t.Status == "in_progress"),
                    PendingTasks = g.Count(t => t.Status == "pending"),
                    CompletionRate = g.Any() ? (g.Count(t => t.Status == "completed") * 100.0) / g.Count() : 0
                })
                .OrderByDescending(x => x.TotalTasks)
                .ToList();

            return breakdown;
        }

        public async Task<IEnumerable<object>> GetPriorityBreakdownAsync(Guid userId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .ToListAsync();

            var breakdown = tasks
                .GroupBy(t => t.PriorityLevel)
                .Select(g => new
                {
                    Priority = g.Key,
                    TotalTasks = g.Count(),
                    CompletedTasks = g.Count(t => t.Status == "completed"),
                    CompletionRate = g.Any() ? (g.Count(t => t.Status == "completed") * 100.0) / g.Count() : 0,
                    AverageCompletionTime = g.Where(t => t.CompletedAt.HasValue)
                       .Average(t => (t.CompletedAt.Value - t.CreatedAt).TotalDays)
                })
                .OrderBy(x => x.Priority switch
                {
                    "critical" => 1,
                    "high" => 2,
                    "medium" => 3,
                    "low" => 4,
                    _ => 5
                })
                .ToList();

            return breakdown;
        }

        public async Task<IEnumerable<object>> GetMonthlyTrendAsync(Guid userId, int months = 6)
        {
            var endDate = DateTime.UtcNow.Date; // Changed to DateTime
            var startDate = endDate.AddMonths(-months);

            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.CreatedAt >= startDate)
                .ToListAsync();

            var monthlyTrend = new List<object>();

            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = endDate.AddMonths(-i).AddDays(1 - endDate.AddMonths(-i).Day);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthTasks = tasks.Where(t =>
                    t.CreatedAt.Date >= monthStart &&
                    t.CreatedAt.Date <= monthEnd).ToList();

                var completedTasks = monthTasks.Where(t => t.Status == "completed").ToList();

                monthlyTrend.Add(new
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    TotalTasks = monthTasks.Count,
                    CompletedTasks = completedTasks.Count,
                    CompletionRate = monthTasks.Any() ? (completedTasks.Count * 100.0) / monthTasks.Count : 0,
                    MonthNumber = monthStart.Month,
                    Year = monthStart.Year
                });
            }

            return monthlyTrend;
        }

        private async Task<int> CalculateCurrentStreakAsync(Guid userId)
        {
            var today = DateTime.UtcNow.Date; // Changed to DateTime
            var streak = 0;

            // Check consecutive days with completed tasks
            for (int i = 0; i < 365; i++) // Check up to a year
            {
                var date = today.AddDays(-i);
                var hasCompletedTask = await _context.Tasks
                    .AnyAsync(t => t.UserId == userId &&
                                  !t.IsDeleted &&
                                  t.Status == "completed" &&
                                  t.CompletedAt.HasValue &&
                                  t.CompletedAt.Value.Date == date);

                if (hasCompletedTask)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        private async Task<int> CalculateBestStreakAsync(Guid userId)
        {
            var completedTasks = await _context.Tasks
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Status == "completed" &&
                           t.CompletedAt.HasValue)
                .Select(t => t.CompletedAt!.Value.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            if (!completedTasks.Any())
                return 0;

            int bestStreak = 1;
            int currentStreak = 1;
            var previousDate = completedTasks.First();

            foreach (var date in completedTasks.Skip(1))
            {
                if ((date - previousDate).Days == 1)
                {
                    currentStreak++;
                    bestStreak = Math.Max(bestStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
                previousDate = date;
            }

            return bestStreak;
        }

        private string GetDueStatus(DateTime? dueDate)
        {
            if (!dueDate.HasValue)
                return "no_due_date";

            var today = DateTime.UtcNow.Date;
            var due = dueDate.Value.Date;

            if (due < today)
                return "overdue";
            if (due == today)
                return "today";
            if (due == today.AddDays(1))
                return "tomorrow";
            if (due <= today.AddDays(7))
                return "this_week";

            return "upcoming";
        }

        private string GetTimeUntil(DateTimeOffset dateTime)
        {
            var timeSpan = dateTime - DateTimeOffset.UtcNow;

            if (timeSpan.TotalDays >= 1)
                return $"in {Math.Ceiling(timeSpan.TotalDays)} days";
            if (timeSpan.TotalHours >= 1)
                return $"in {Math.Ceiling(timeSpan.TotalHours)} hours";
            if (timeSpan.TotalMinutes >= 1)
                return $"in {Math.Ceiling(timeSpan.TotalMinutes)} minutes";

            return "now";
        }

        private string GetTaskAction(TaskEntity task)
        {
            if (task.Status == "completed")
                return "completed";
            if (task.UpdatedAt > task.CreatedAt.AddMinutes(5)) // Significant update
                return "updated";

            return "created";
        }

        private string GetAppointmentAction(Appointment appointment)
        {
            if (appointment.Status == "completed")
                return "completed";
            if (appointment.Status == "cancelled")
                return "cancelled";
            if (appointment.UpdatedAt > appointment.CreatedAt.AddMinutes(5))
                return "updated";

            return "created";
        }

        private async Task<string?> GetUserEmailAsync(Guid userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
        }
    }
}