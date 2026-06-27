// Application/Services/TaskManagementService.cs (renamed to avoid conflict)
using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Services
{
    public class TaskManagementService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaskEntity> GetTaskByIdAsync(Guid TaskID)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.Reminders)
                .Include(t => t.ChildTasks)
                .Include(t => t.CategoryNavigation)
                .FirstOrDefaultAsync(t => t.TaskID == TaskID && !t.IsDeleted);
        }

        public async Task<IEnumerable<TaskEntity>> GetUserTasksAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID && !t.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(t => t.DueDate >= startDate || t.DueDate == null);

            if (endDate.HasValue)
                query = query.Where(t => t.DueDate <= endDate || t.DueDate == null);

            return await query
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<TaskEntity> CreateTaskAsync(TaskEntity task)
        {
            task.TaskID = Guid.NewGuid();
            task.CreatedAt = DateTimeOffset.UtcNow;
            task.UpdatedAt = DateTimeOffset.UtcNow;
            task.IsDeleted = false;
            task.DeletedAt = null;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskEntity> UpdateTaskAsync(TaskEntity task)
        {
            var existing = await GetTaskByIdAsync(task.TaskID);
            if (existing == null)
                throw new KeyNotFoundException($"Task with ID {task.TaskID} not found");

            task.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> DeleteTaskAsync(Guid TaskID)
        {
            var task = await GetTaskByIdAsync(TaskID);
            if (task == null) return false;

            task.IsDeleted = true;
            task.DeletedAt = DateTimeOffset.UtcNow;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var subtask in task.Subtasks)
            {
                subtask.IsDeleted = true;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(Guid UserID, DateTime startDate, DateTime endDate)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.DueDate >= startDate &&
                           t.DueDate <= endDate)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(Guid UserID, string status)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.Status == status)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Guid UserID, string priority)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.PriorityLevel == priority)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByCategoryAsync(Guid UserID, string category)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.CategoryNavigation != null &&
                           t.CategoryNavigation.CategoryName == category)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(Guid UserID, string taskType)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.TaskType == taskType)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(Guid UserID)
        {
            var today = DateTime.Today;
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.Status != "completed" &&
                           t.Status != "cancelled" &&
                           t.DueDate < today)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTodayTasksAsync(Guid UserID)
        {
            var today = DateTime.Today;
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.DueDate == today)
                .OrderByDescending(t => t.PriorityLevel)
                .ThenBy(t => t.DueTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetUpcomingTasksAsync(Guid UserID, int daysAhead = 7)
        {
            var today = DateTime.Today;
            var futureDate = today.AddDays(daysAhead);

            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.Status != "completed" &&
                           t.Status != "cancelled" &&
                           t.DueDate >= today &&
                           t.DueDate <= futureDate)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksWithSubtasksAsync(Guid UserID)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.Subtasks.Any())
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetRecurringTasksAsync(Guid UserID)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.IsRecurring)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<int> GetTaskCountByUserAsync(Guid UserID)
        {
            return await _context.Tasks
                .CountAsync(t => t.UserID == UserID && !t.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetTaskStatisticsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID && !t.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate);

            var tasks = await query.ToListAsync();

            return new Dictionary<string, int>
            {
                { "total", tasks.Count },
                { "pending", tasks.Count(t => t.Status == "pending") },
                { "in_progress", tasks.Count(t => t.Status == "in_progress") },
                { "completed", tasks.Count(t => t.Status == "completed") },
                { "cancelled", tasks.Count(t => t.Status == "cancelled") },
                { "overdue", tasks.Count(t => t.Status != "completed" && t.Status != "cancelled" && t.DueDate < DateTime.Today) },
                { "today", tasks.Count(t => t.DueDate == DateTime.Today && t.Status != "completed") },
                { "with_subtasks", tasks.Count(t => t.Subtasks.Any()) },
                { "recurring", tasks.Count(t => t.IsRecurring) }
            };
        }

        public async Task<Dictionary<string, object>> GetTaskCompletionStatsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID && !t.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate);

            var tasks = await query.ToListAsync();
            var completedTasks = tasks.Where(t => t.Status == "completed").ToList();

            // Fixed: Use category name from navigation
            var categoryBreakdown = tasks
                .GroupBy(t => t.CategoryNavigation != null ? t.CategoryNavigation.CategoryName : "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Count());

            return new Dictionary<string, object>
            {
                { "totalTasks", tasks.Count },
                { "completedTasks", completedTasks.Count },
                { "completionRate", tasks.Count > 0 ? (completedTasks.Count * 100.0 / tasks.Count) : 0 },
                { "avgCompletionPercentage", tasks.Count > 0 ? tasks.Average(t => t.CompletionPercentage) : 0 },
                { "totalTimeSpent", tasks.Sum(t => t.TimeSpentMinutes) },
                { "avgTimeSpent", tasks.Count > 0 ? tasks.Average(t => t.TimeSpentMinutes) : 0 },
                { "priorityBreakdown", tasks.GroupBy(t => t.PriorityLevel)
                    .ToDictionary(g => g.Key, g => g.Count()) },
                { "categoryBreakdown", categoryBreakdown }
            };
        }

        public async Task<bool> ChangeTaskStatusAsync(Guid TaskID, string newStatus, int? completionPercentage = null)
        {
            var task = await GetTaskByIdAsync(TaskID);
            if (task == null) return false;

            task.Status = newStatus;

            if (completionPercentage.HasValue)
                task.CompletionPercentage = completionPercentage.Value;

            if (newStatus == "completed")
            {
                task.CompletedAt = DateTimeOffset.UtcNow;
                task.CompletionPercentage = 100;

                foreach (var subtask in task.Subtasks.Where(s => s.Status != "completed"))
                {
                    subtask.Status = "completed";
                    subtask.CompletedAt = DateTimeOffset.UtcNow;
                }
            }

            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTaskProgressAsync(Guid TaskID, int progressPercentage)
        {
            var task = await GetTaskByIdAsync(TaskID);
            if (task == null) return false;

            if (progressPercentage < 0 || progressPercentage > 100)
                throw new ArgumentException("Progress percentage must be between 0 and 100");

            task.CompletionPercentage = progressPercentage;

            if (progressPercentage == 100)
            {
                task.Status = "completed";
                task.CompletedAt = DateTimeOffset.UtcNow;
            }
            else if (progressPercentage > 0 && task.Status == "pending")
            {
                task.Status = "in_progress";
            }

            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddTimeSpentAsync(Guid TaskID, int minutes)
        {
            var task = await GetTaskByIdAsync(TaskID);
            if (task == null) return false;

            task.TimeSpentMinutes += minutes;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TaskEntity> DuplicateTaskAsync(Guid TaskID)
        {
            var original = await GetTaskByIdAsync(TaskID);
            if (original == null)
                throw new KeyNotFoundException($"Task with ID {TaskID} not found");

            var duplicate = new TaskEntity
            {
                UserID = original.UserID,
                Title = $"{original.Title} (Copy)",
                Description = original.Description,
                CategoryID = original.CategoryID,
                TaskType = original.TaskType,
                PriorityLevel = original.PriorityLevel,
                Status = "pending",
                CompletionPercentage = 0,
                DueDate = original.DueDate,
                DueTime = original.DueTime,
                EstimatedDurationMinutes = original.EstimatedDurationMinutes,
                Tags = original.Tags,
                Notes = original.Notes,
                LocationName = original.LocationName,
                LocationAddress = original.LocationAddress,
                IsRecurring = original.IsRecurring,
                RecurrenceRule = original.RecurrenceRule
            };

            return await CreateTaskAsync(duplicate);
        }

        public async Task<IEnumerable<TaskEntity>> SearchTasksAsync(Guid UserID, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetUserTasksAsync(UserID);

            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           (t.Title.Contains(searchTerm) ||
                            t.Description.Contains(searchTerm) ||
                            t.Tags.Contains(searchTerm) ||
                            t.Notes.Contains(searchTerm) ||
                            t.LocationName.Contains(searchTerm)))
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksWithRemindersAsync(Guid UserID)
        {
            return await _context.Tasks
                .Include(t => t.Reminders)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           t.Reminders.Any())
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(Guid UserID, string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return await GetUserTasksAsync(UserID);

            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.CategoryNavigation)
                .Where(t => t.UserID == UserID &&
                           !t.IsDeleted &&
                           tags.Any(tag => t.Tags.Contains(tag)))
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<bool> AddSubtaskToTaskAsync(Guid TaskID, Subtask subtask)
        {
            var task = await GetTaskByIdAsync(TaskID);
            if (task == null) return false;

            subtask.SubTaskID = Guid.NewGuid();
            subtask.TaskID = TaskID;
            subtask.CreatedAt = DateTimeOffset.UtcNow;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;
            subtask.IsDeleted = false;

            task.Subtasks.Add(subtask);
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveSubtaskFromTaskAsync(Guid TaskID, Guid subTaskID)
        {
            var task = await GetTaskByIdAsync(TaskID);
            if (task == null) return false;

            var subtask = task.Subtasks.FirstOrDefault(s => s.SubTaskID == subTaskID);
            if (subtask == null) return false;

            subtask.IsDeleted = true;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid TaskID)
        {
            var task = await GetTaskByIdAsync(TaskID);
            return task?.Subtasks.Where(s => !s.IsDeleted).ToList() ?? new List<Subtask>();
        }

        public async Task<bool> CompleteMultipleTasksAsync(Guid[] TaskIDs)
        {
            var tasks = await _context.Tasks
                .Where(t => TaskIDs.Contains(t.TaskID) && !t.IsDeleted)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.Status = "completed";
                task.CompletionPercentage = 100;
                task.CompletedAt = DateTimeOffset.UtcNow;
                task.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMultipleTasksAsync(Guid[] TaskIDs)
        {
            var tasks = await _context.Tasks
                .Where(t => TaskIDs.Contains(t.TaskID) && !t.IsDeleted)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.IsDeleted = true;
                task.DeletedAt = DateTimeOffset.UtcNow;
                task.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeMultipleTasksPriorityAsync(Guid[] TaskIDs, string newPriority)
        {
            var tasks = await _context.Tasks
                .Where(t => TaskIDs.Contains(t.TaskID) && !t.IsDeleted)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.PriorityLevel = newPriority;
                task.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}