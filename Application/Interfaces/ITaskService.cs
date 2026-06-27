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
        Task<TaskEntity> GetTaskByIdAsync(Guid TaskID);
        Task<IEnumerable<TaskEntity>> GetUserTasksAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<TaskEntity> CreateTaskAsync(TaskEntity task);
        Task<TaskEntity> UpdateTaskAsync(TaskEntity task);
        Task<bool> DeleteTaskAsync(Guid TaskID);

        // Query operations
        Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(Guid UserID, DateTime startDate, DateTime endDate);
        Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(Guid UserID, string status);
        Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Guid UserID, string priority);
        Task<IEnumerable<TaskEntity>> GetTasksByCategoryAsync(Guid UserID, string category);
        Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(Guid UserID, string taskType);
        Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(Guid UserID);
        Task<IEnumerable<TaskEntity>> GetTodayTasksAsync(Guid UserID);
        Task<IEnumerable<TaskEntity>> GetUpcomingTasksAsync(Guid UserID, int daysAhead = 7);
        Task<IEnumerable<TaskEntity>> GetTasksWithSubtasksAsync(Guid UserID);
        Task<IEnumerable<TaskEntity>> GetRecurringTasksAsync(Guid UserID);

        // Statistics
        Task<int> GetTaskCountByUserAsync(Guid UserID);
        Task<Dictionary<string, int>> GetTaskStatisticsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> GetTaskCompletionStatsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);

        // Special operations
        Task<bool> ChangeTaskStatusAsync(Guid TaskID, string newStatus, int? completionPercentage = null);
        Task<bool> UpdateTaskProgressAsync(Guid TaskID, int progressPercentage);
        Task<bool> AddTimeSpentAsync(Guid TaskID, int minutes);
        Task<TaskEntity> DuplicateTaskAsync(Guid TaskID);
        Task<IEnumerable<TaskEntity>> SearchTasksAsync(Guid UserID, string searchTerm);
        Task<IEnumerable<TaskEntity>> GetTasksWithRemindersAsync(Guid UserID);
        Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(Guid UserID, string[] tags);

        // Subtask operations
        Task<bool> AddSubtaskToTaskAsync(Guid TaskID, Subtask subtask);
        Task<bool> RemoveSubtaskFromTaskAsync(Guid TaskID, Guid subTaskID);
        Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid TaskID);

        // Bulk operations
        Task<bool> CompleteMultipleTasksAsync(Guid[] TaskIDs);
        Task<bool> DeleteMultipleTasksAsync(Guid[] TaskIDs);
        Task<bool> ChangeMultipleTasksPriorityAsync(Guid[] TaskIDs, string newPriority);
    }
}