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

        public async Task<TaskEntity> GetTaskByIdAsync(Guid taskId)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Include(t => t.Reminders)
                .Include(t => t.ChildTasks)
                .FirstOrDefaultAsync(t => t.TaskId == taskId && !t.IsDeleted);
        }

        public async Task<IEnumerable<TaskEntity>> GetUserTasksAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId && !t.IsDeleted);

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
            task.TaskId = Guid.NewGuid();
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
            var existing = await GetTaskByIdAsync(task.TaskId);
            if (existing == null)
                throw new KeyNotFoundException($"Task with ID {task.TaskId} not found");

            task.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return false;

            task.IsDeleted = true;
            task.DeletedAt = DateTimeOffset.UtcNow;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            // Soft delete subtasks
            foreach (var subtask in task.Subtasks)
            {
                subtask.IsDeleted = true;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.DueDate >= startDate &&
                           t.DueDate <= endDate)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(Guid userId, string status)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Status == status)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Guid userId, string priority)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.PriorityLevel == priority)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByCategoryAsync(Guid userId, string category)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Category == category)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(Guid userId, string taskType)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.TaskType == taskType)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(Guid userId)
        {
            var today = DateTime.Today;
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Status != "completed" &&
                           t.Status != "cancelled" &&
                           t.DueDate < today)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTodayTasksAsync(Guid userId)
        {
            var today = DateTime.Today;
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.DueDate == today)
                .OrderByDescending(t => t.PriorityLevel)
                .ThenBy(t => t.DueTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetUpcomingTasksAsync(Guid userId, int daysAhead = 7)
        {
            var today = DateTime.Today;
            var futureDate = today.AddDays(daysAhead);

            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Status != "completed" &&
                           t.Status != "cancelled" &&
                           t.DueDate >= today &&
                           t.DueDate <= futureDate)
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksWithSubtasksAsync(Guid userId)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Subtasks.Any())
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetRecurringTasksAsync(Guid userId)
        {
            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.IsRecurring)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<int> GetTaskCountByUserAsync(Guid userId)
        {
            return await _context.Tasks
                .CountAsync(t => t.UserId == userId && !t.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetTaskStatisticsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted);

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

        public async Task<Dictionary<string, object>> GetTaskCompletionStatsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate);

            var tasks = await query.ToListAsync();
            var completedTasks = tasks.Where(t => t.Status == "completed").ToList();

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
                { "categoryBreakdown", tasks.GroupBy(t => t.Category)
                    .ToDictionary(g => g.Key, g => g.Count()) }
            };
        }

        public async Task<bool> ChangeTaskStatusAsync(Guid taskId, string newStatus, int? completionPercentage = null)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return false;

            task.Status = newStatus;

            if (completionPercentage.HasValue)
                task.CompletionPercentage = completionPercentage.Value;

            if (newStatus == "completed")
            {
                task.CompletedAt = DateTimeOffset.UtcNow;
                task.CompletionPercentage = 100;

                // Complete all subtasks
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

        public async Task<bool> UpdateTaskProgressAsync(Guid taskId, int progressPercentage)
        {
            var task = await GetTaskByIdAsync(taskId);
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

        public async Task<bool> AddTimeSpentAsync(Guid taskId, int minutes)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return false;

            task.TimeSpentMinutes += minutes;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TaskEntity> DuplicateTaskAsync(Guid taskId)
        {
            var original = await GetTaskByIdAsync(taskId);
            if (original == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found");

            var duplicate = new TaskEntity
            {
                UserId = original.UserId,
                Title = $"{original.Title} (Copy)",
                Description = original.Description,
                Category = original.Category,
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

        public async Task<IEnumerable<TaskEntity>> SearchTasksAsync(Guid userId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetUserTasksAsync(userId);

            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
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

        public async Task<IEnumerable<TaskEntity>> GetTasksWithRemindersAsync(Guid userId)
        {
            return await _context.Tasks
                .Include(t => t.Reminders)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           t.Reminders.Any())
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(Guid userId, string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return await GetUserTasksAsync(userId);

            return await _context.Tasks
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId &&
                           !t.IsDeleted &&
                           tags.Any(tag => t.Tags.Contains(tag)))
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.PriorityLevel)
                .ToListAsync();
        }

        public async Task<bool> AddSubtaskToTaskAsync(Guid taskId, Subtask subtask)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return false;

            subtask.SubtaskId = Guid.NewGuid();
            subtask.TaskId = taskId;
            subtask.CreatedAt = DateTimeOffset.UtcNow;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;
            subtask.IsDeleted = false;

            task.Subtasks.Add(subtask);
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveSubtaskFromTaskAsync(Guid taskId, Guid subtaskId)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return false;

            var subtask = task.Subtasks.FirstOrDefault(s => s.SubtaskId == subtaskId);
            if (subtask == null) return false;

            subtask.IsDeleted = true;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid taskId)
        {
            var task = await GetTaskByIdAsync(taskId);
            return task?.Subtasks.Where(s => !s.IsDeleted).ToList() ?? new List<Subtask>();
        }

        public async Task<bool> CompleteMultipleTasksAsync(Guid[] taskIds)
        {
            var tasks = await _context.Tasks
                .Where(t => taskIds.Contains(t.TaskId) && !t.IsDeleted)
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

        public async Task<bool> DeleteMultipleTasksAsync(Guid[] taskIds)
        {
            var tasks = await _context.Tasks
                .Where(t => taskIds.Contains(t.TaskId) && !t.IsDeleted)
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

        public async Task<bool> ChangeMultipleTasksPriorityAsync(Guid[] taskIds, string newPriority)
        {
            var tasks = await _context.Tasks
                .Where(t => taskIds.Contains(t.TaskId) && !t.IsDeleted)
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