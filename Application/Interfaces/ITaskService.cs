using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface ITaskService
    {
        // Basic CRUD
        Task<TaskEntity> GetTaskByIdAsync(Guid taskId);
        Task<IEnumerable<TaskEntity>> GetUserTasksAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<TaskEntity> CreateTaskAsync(TaskEntity task);
        Task<TaskEntity> UpdateTaskAsync(TaskEntity task);
        Task<bool> DeleteTaskAsync(Guid taskId);

        // Query operations
        Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(Guid userId, string status);
        Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Guid userId, string priority);
        Task<IEnumerable<TaskEntity>> GetTasksByCategoryAsync(Guid userId, string category);
        Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(Guid userId, string taskType);
        Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(Guid userId);
        Task<IEnumerable<TaskEntity>> GetTodayTasksAsync(Guid userId);
        Task<IEnumerable<TaskEntity>> GetUpcomingTasksAsync(Guid userId, int daysAhead = 7);
        Task<IEnumerable<TaskEntity>> GetTasksWithSubtasksAsync(Guid userId);
        Task<IEnumerable<TaskEntity>> GetRecurringTasksAsync(Guid userId);

        // Statistics
        Task<int> GetTaskCountByUserAsync(Guid userId);
        Task<Dictionary<string, int>> GetTaskStatisticsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> GetTaskCompletionStatsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);

        // Special operations
        Task<bool> ChangeTaskStatusAsync(Guid taskId, string newStatus, int? completionPercentage = null);
        Task<bool> UpdateTaskProgressAsync(Guid taskId, int progressPercentage);
        Task<bool> AddTimeSpentAsync(Guid taskId, int minutes);
        Task<TaskEntity> DuplicateTaskAsync(Guid taskId);
        Task<IEnumerable<TaskEntity>> SearchTasksAsync(Guid userId, string searchTerm);
        Task<IEnumerable<TaskEntity>> GetTasksWithRemindersAsync(Guid userId);
        Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(Guid userId, string[] tags);

        // Subtask operations
        Task<bool> AddSubtaskToTaskAsync(Guid taskId, Subtask subtask);
        Task<bool> RemoveSubtaskFromTaskAsync(Guid taskId, Guid subtaskId);
        Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid taskId);

        // Bulk operations
        Task<bool> CompleteMultipleTasksAsync(Guid[] taskIds);
        Task<bool> DeleteMultipleTasksAsync(Guid[] taskIds);
        Task<bool> ChangeMultipleTasksPriorityAsync(Guid[] taskIds, string newPriority);
    }
}