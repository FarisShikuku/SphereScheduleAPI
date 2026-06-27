using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Services
{
    public class SubtaskService : ISubtaskService
    {
        private readonly ApplicationDbContext _context;

        public SubtaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Subtask> GetSubtaskByIdAsync(Guid subTaskID)
        {
            return await _context.Subtasks
                .Include(s => s.Task)
                .FirstOrDefaultAsync(s => s.SubTaskID == subTaskID && !s.IsDeleted);
        }

        public async Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid TaskID)
        {
            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID && !s.IsDeleted)
                .OrderBy(s => s.SubtaskOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Subtask> CreateSubtaskAsync(Subtask subtask)
        {
            // Verify parent task exists
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskID == subtask.TaskID && !t.IsDeleted);

            if (task == null)
                throw new KeyNotFoundException($"Parent task with ID {subtask.TaskID} not found");

            subtask.SubTaskID = Guid.NewGuid();
            subtask.CreatedAt = DateTimeOffset.UtcNow;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;
            subtask.IsDeleted = false;

            // Set order to last if not specified
            if (subtask.SubtaskOrder == 0)
            {
                var maxOrder = await _context.Subtasks
                    .Where(s => s.TaskID == subtask.TaskID && !s.IsDeleted)
                    .MaxAsync(s => (int?)s.SubtaskOrder) ?? 0;
                subtask.SubtaskOrder = maxOrder + 1;
            }

            _context.Subtasks.Add(subtask);
            await _context.SaveChangesAsync();
            return subtask;
        }

        public async Task<Subtask> UpdateSubtaskAsync(Subtask subtask)
        {
            var existing = await GetSubtaskByIdAsync(subtask.SubTaskID);
            if (existing == null)
                throw new KeyNotFoundException($"Subtask with ID {subtask.SubTaskID} not found");

            subtask.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(subtask);
            await _context.SaveChangesAsync();
            return subtask;
        }

        public async Task<bool> DeleteSubtaskAsync(Guid subTaskID)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            subtask.IsDeleted = true;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreSubtaskAsync(Guid subTaskID)
        {
            var subtask = await _context.Subtasks
                .FirstOrDefaultAsync(s => s.SubTaskID == subTaskID && s.IsDeleted);

            if (subtask == null) return false;

            subtask.IsDeleted = false;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Subtask>> GetSubtasksByStatusAsync(Guid TaskID, string status)
        {
            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.Status == status)
                .OrderBy(s => s.SubtaskOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetSubtasksByPriorityAsync(Guid TaskID, string priority)
        {
            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.Priority == priority)
                .OrderBy(s => s.SubtaskOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetOverdueSubtasksAsync(Guid TaskID)
        {
            var today = DateTime.Today;
            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.Status != "completed" &&
                           s.Status != "cancelled" &&
                           s.DueDate < today)
                .OrderBy(s => s.DueDate)
                .ThenBy(s => s.SubtaskOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetTodaySubtasksAsync(Guid TaskID)
        {
            var today = DateTime.Today;
            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.DueDate == today)
                .OrderBy(s => s.DueTime)
                .ThenBy(s => s.SubtaskOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetUpcomingSubtasksAsync(Guid TaskID, int daysAhead = 7)
        {
            var today = DateTime.Today;
            var futureDate = today.AddDays(daysAhead);

            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.Status != "completed" &&
                           s.Status != "cancelled" &&
                           s.DueDate >= today &&
                           s.DueDate <= futureDate)
                .OrderBy(s => s.DueDate)
                .ThenBy(s => s.SubtaskOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetCompletedSubtasksAsync(Guid TaskID, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.Status == "completed");

            if (startDate.HasValue)
                query = query.Where(s => s.CompletedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(s => s.CompletedAt <= endDate);

            return await query
                .OrderByDescending(s => s.CompletedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetSubtasksByDateRangeAsync(Guid TaskID, DateTime startDate, DateTime endDate)
        {
            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.DueDate >= startDate &&
                           s.DueDate <= endDate)
                .OrderBy(s => s.DueDate)
                .ThenBy(s => s.SubtaskOrder)
                .ToListAsync();
        }

        public async Task<bool> ChangeSubtaskStatusAsync(Guid subTaskID, string newStatus)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            subtask.Status = newStatus;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;

            if (newStatus == "completed")
            {
                subtask.CompletedAt = DateTimeOffset.UtcNow;
            }
            else if (newStatus != "completed" && subtask.CompletedAt.HasValue)
            {
                subtask.CompletedAt = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteSubtaskAsync(Guid subTaskID)
        {
            return await ChangeSubtaskStatusAsync(subTaskID, "completed");
        }

        public async Task<bool> UpdateSubtaskProgressAsync(Guid subTaskID, string status)
        {
            return await ChangeSubtaskStatusAsync(subTaskID, status);
        }

        public async Task<bool> UpdateSubtaskPriorityAsync(Guid subTaskID, string newPriority)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            subtask.Priority = newPriority;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSubtaskOrderAsync(Guid subTaskID, int newOrder)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            // Get all subtasks for this task
            var subtasks = await _context.Subtasks
                .Where(s => s.TaskID == subtask.TaskID && !s.IsDeleted)
                .OrderBy(s => s.SubtaskOrder)
                .ToListAsync();

            // Remove subtask from list and insert at new position
            var subtaskToMove = subtasks.FirstOrDefault(s => s.SubTaskID == subTaskID);
            if (subtaskToMove == null) return false;

            subtasks.Remove(subtaskToMove);
            newOrder = Math.Max(0, Math.Min(newOrder, subtasks.Count));
            subtasks.Insert(newOrder, subtaskToMove);

            // Update orders
            for (int i = 0; i < subtasks.Count; i++)
            {
                subtasks[i].SubtaskOrder = i;
                subtasks[i].UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderSubtasksAsync(Guid TaskID, Dictionary<Guid, int> subtaskOrders)
        {
            var subtasks = await _context.Subtasks
                .Where(s => s.TaskID == TaskID && !s.IsDeleted)
                .ToListAsync();

            foreach (var subtask in subtasks)
            {
                if (subtaskOrders.TryGetValue(subtask.SubTaskID, out int newOrder))
                {
                    subtask.SubtaskOrder = newOrder;
                    subtask.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSubtaskDueDateAsync(Guid subTaskID, DateTime? dueDate, TimeSpan? dueTime = null)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            subtask.DueDate = dueDate;
            subtask.DueTime = dueTime;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetSubtaskCountAsync(Guid TaskID)
        {
            return await _context.Subtasks
                .CountAsync(s => s.TaskID == TaskID && !s.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetSubtaskStatisticsAsync(Guid TaskID)
        {
            var subtasks = await GetTaskSubtasksAsync(TaskID);
            var subtaskList = subtasks.ToList();

            return new Dictionary<string, int>
            {
                { "total", subtaskList.Count },
                { "pending", subtaskList.Count(s => s.Status == "pending") },
                { "in_progress", subtaskList.Count(s => s.Status == "in_progress") },
                { "completed", subtaskList.Count(s => s.Status == "completed") },
                { "cancelled", subtaskList.Count(s => s.Status == "cancelled") },
                { "overdue", subtaskList.Count(s => s.Status != "completed" && s.Status != "cancelled" && s.DueDate < DateTime.Today) },
                { "today", subtaskList.Count(s => s.DueDate == DateTime.Today && s.Status != "completed") },
                { "high_priority", subtaskList.Count(s => s.Priority == "high") },
                { "medium_priority", subtaskList.Count(s => s.Priority == "medium") },
                { "low_priority", subtaskList.Count(s => s.Priority == "low") }
            };
        }

        public async Task<decimal> GetSubtaskCompletionRateAsync(Guid TaskID)
        {
            var subtasks = await GetTaskSubtasksAsync(TaskID);
            var subtaskList = subtasks.ToList();

            if (!subtaskList.Any())
                return 0;

            var completed = subtaskList.Count(s => s.Status == "completed");
            return (decimal)completed / subtaskList.Count * 100;
        }

        public async Task<TimeSpan?> GetAverageCompletionTimeAsync(Guid TaskID)
        {
            var completedSubtasks = await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           s.Status == "completed" &&
                           s.CompletedAt.HasValue)
                .ToListAsync();

            if (!completedSubtasks.Any())
                return null;

            var totalTime = TimeSpan.Zero;
            foreach (var subtask in completedSubtasks)
            {
                if (subtask.CompletedAt.HasValue)
                {
                    totalTime += subtask.CompletedAt.Value - subtask.CreatedAt;
                }
            }

            return TimeSpan.FromTicks(totalTime.Ticks / completedSubtasks.Count);
        }


        public async Task<bool> CreateMultipleSubtasksAsync(Guid TaskID, IEnumerable<Subtask> subtasks)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskID == TaskID && !t.IsDeleted);

            if (task == null) return false;

            var subtaskList = subtasks.ToList();
            var existingCount = await GetSubtaskCountAsync(TaskID);

            for (int i = 0; i < subtaskList.Count; i++)
            {
                var subtask = subtaskList[i];
                subtask.TaskID = TaskID;
                subtask.SubTaskID = Guid.NewGuid();
                subtask.CreatedAt = DateTimeOffset.UtcNow;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;
                subtask.IsDeleted = false;
                subtask.SubtaskOrder = existingCount + i + 1;

                _context.Subtasks.Add(subtask);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMultipleSubtasksAsync(Guid[] subTaskIDs)
        {
            var subtasks = await _context.Subtasks
                .Where(s => subTaskIDs.Contains(s.SubTaskID) && !s.IsDeleted)
                .ToListAsync();

            foreach (var subtask in subtasks)
            {
                subtask.IsDeleted = true;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteMultipleSubtasksAsync(Guid[] subTaskIDs)
        {
            var subtasks = await _context.Subtasks
                .Where(s => subTaskIDs.Contains(s.SubTaskID) && !s.IsDeleted)
                .ToListAsync();

            foreach (var subtask in subtasks)
            {
                subtask.Status = "completed";
                subtask.CompletedAt = DateTimeOffset.UtcNow;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeMultipleSubtasksStatusAsync(Guid[] subTaskIDs, string newStatus)
        {
            var subtasks = await _context.Subtasks
                .Where(s => subTaskIDs.Contains(s.SubTaskID) && !s.IsDeleted)
                .ToListAsync();

            foreach (var subtask in subtasks)
            {
                subtask.Status = newStatus;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;

                if (newStatus == "completed" && !subtask.CompletedAt.HasValue)
                {
                    subtask.CompletedAt = DateTimeOffset.UtcNow;
                }
                else if (newStatus != "completed" && subtask.CompletedAt.HasValue)
                {
                    subtask.CompletedAt = null;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeMultipleSubtasksPriorityAsync(Guid[] subTaskIDs, string newPriority)
        {
            var subtasks = await _context.Subtasks
                .Where(s => subTaskIDs.Contains(s.SubTaskID) && !s.IsDeleted)
                .ToListAsync();

            foreach (var subtask in subtasks)
            {
                subtask.Priority = newPriority;
                subtask.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveSubtaskToTaskAsync(Guid subTaskID, Guid newTaskID)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            // Verify new task exists
            var newTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskID == newTaskID && !t.IsDeleted);

            if (newTask == null) return false;

            // Get new order for the subtask in the new task
            var newOrder = await _context.Subtasks
                .Where(s => s.TaskID == newTaskID && !s.IsDeleted)
                .MaxAsync(s => (int?)s.SubtaskOrder) ?? 0;

            subtask.TaskID = newTaskID;
            subtask.SubtaskOrder = newOrder + 1;
            subtask.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CopySubtaskToTaskAsync(Guid subTaskID, Guid newTaskID)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            if (subtask == null) return false;

            // Verify new task exists
            var newTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskID == newTaskID && !t.IsDeleted);

            if (newTask == null) return false;

            // Create a copy
            var copy = new Subtask
            {
                TaskID = newTaskID,
                Title = $"{subtask.Title} (Copy)",
                Description = subtask.Description,
                Status = "pending",
                Priority = subtask.Priority,
                DueDate = subtask.DueDate,
                DueTime = subtask.DueTime,
                SubtaskOrder = await GetSubtaskCountAsync(newTaskID) + 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            _context.Subtasks.Add(copy);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Subtask>> GetSubtasksWithParentInfoAsync(Guid TaskID)
        {
            return await _context.Subtasks
                .Include(s => s.Task)
                .Where(s => s.TaskID == TaskID && !s.IsDeleted)
                .OrderBy(s => s.SubtaskOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> SearchSubtasksAsync(Guid TaskID, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetTaskSubtasksAsync(TaskID);

            return await _context.Subtasks
                .Where(s => s.TaskID == TaskID &&
                           !s.IsDeleted &&
                           (s.Title.Contains(searchTerm) ||
                            s.Description.Contains(searchTerm)))
                .OrderBy(s => s.SubtaskOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> FilterSubtasksAsync(Guid TaskID, SubtaskFilterDto filter)
        {
            var query = _context.Subtasks
                .Where(s => s.TaskID == TaskID && !s.IsDeleted);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(s => s.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Priority))
                query = query.Where(s => s.Priority == filter.Priority);

            if (filter.DueDateFrom.HasValue)
                query = query.Where(s => s.DueDate >= filter.DueDateFrom);

            if (filter.DueDateTo.HasValue)
                query = query.Where(s => s.DueDate <= filter.DueDateTo);

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
                query = query.Where(s => s.Status != "completed" && s.Status != "cancelled" && s.DueDate < DateTime.Today);

            return await query
                .OrderBy(s => s.SubtaskOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CanDeleteSubtaskAsync(Guid subTaskID)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            return subtask != null; // All subtasks can be deleted
        }

        public async Task<bool> SubtaskBelongsToTaskAsync(Guid subTaskID, Guid TaskID)
        {
            var subtask = await GetSubtaskByIdAsync(subTaskID);
            return subtask != null && subtask.TaskID == TaskID;
        }

        public async Task<bool> SubtaskExistsAsync(Guid subTaskID)
        {
            return await _context.Subtasks
                .AnyAsync(s => s.SubTaskID == subTaskID && !s.IsDeleted);
        }
    }
}