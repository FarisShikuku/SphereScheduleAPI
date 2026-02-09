using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface ISubtaskService
    {
        // Basic CRUD
        Task<Subtask> GetSubtaskByIdAsync(Guid subtaskId);
        Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid taskId);
        Task<Subtask> CreateSubtaskAsync(Subtask subtask);
        Task<Subtask> UpdateSubtaskAsync(Subtask subtask);
        Task<bool> DeleteSubtaskAsync(Guid subtaskId);
        Task<bool> RestoreSubtaskAsync(Guid subtaskId);

        // Query operations
        Task<IEnumerable<Subtask>> GetSubtasksByStatusAsync(Guid taskId, string status);
        Task<IEnumerable<Subtask>> GetSubtasksByPriorityAsync(Guid taskId, string priority);
        Task<IEnumerable<Subtask>> GetOverdueSubtasksAsync(Guid taskId);
        Task<IEnumerable<Subtask>> GetTodaySubtasksAsync(Guid taskId);
        Task<IEnumerable<Subtask>> GetUpcomingSubtasksAsync(Guid taskId, int daysAhead = 7);
        Task<IEnumerable<Subtask>> GetCompletedSubtasksAsync(Guid taskId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<Subtask>> GetSubtasksByDateRangeAsync(Guid taskId, DateTime startDate, DateTime endDate);

        // Subtask management
        Task<bool> ChangeSubtaskStatusAsync(Guid subtaskId, string newStatus);
        Task<bool> CompleteSubtaskAsync(Guid subtaskId);
        Task<bool> UpdateSubtaskProgressAsync(Guid subtaskId, string status);
        Task<bool> UpdateSubtaskPriorityAsync(Guid subtaskId, string newPriority);
        Task<bool> UpdateSubtaskOrderAsync(Guid subtaskId, int newOrder);
        Task<bool> ReorderSubtasksAsync(Guid taskId, Dictionary<Guid, int> subtaskOrders);
        Task<bool> UpdateSubtaskDueDateAsync(Guid subtaskId, DateTime? dueDate, TimeSpan? dueTime = null);

        // Statistics
        Task<int> GetSubtaskCountAsync(Guid taskId);
        Task<Dictionary<string, int>> GetSubtaskStatisticsAsync(Guid taskId);
        Task<decimal> GetSubtaskCompletionRateAsync(Guid taskId);
        Task<TimeSpan?> GetAverageCompletionTimeAsync(Guid taskId);

        // Bulk operations
        Task<bool> CreateMultipleSubtasksAsync(Guid taskId, IEnumerable<Subtask> subtasks);
        Task<bool> DeleteMultipleSubtasksAsync(Guid[] subtaskIds);
        Task<bool> CompleteMultipleSubtasksAsync(Guid[] subtaskIds);
        Task<bool> ChangeMultipleSubtasksStatusAsync(Guid[] subtaskIds, string newStatus);
        Task<bool> ChangeMultipleSubtasksPriorityAsync(Guid[] subtaskIds, string newPriority);

        // Task relationship
        Task<bool> MoveSubtaskToTaskAsync(Guid subtaskId, Guid newTaskId);
        Task<bool> CopySubtaskToTaskAsync(Guid subtaskId, Guid newTaskId);
        Task<IEnumerable<Subtask>> GetSubtasksWithParentInfoAsync(Guid taskId);

        // Search and filter
        Task<IEnumerable<Subtask>> SearchSubtasksAsync(Guid taskId, string searchTerm);
        Task<IEnumerable<Subtask>> FilterSubtasksAsync(Guid taskId, SubtaskFilterDto filter);

        // Validation
        Task<bool> CanDeleteSubtaskAsync(Guid subtaskId);
        Task<bool> SubtaskBelongsToTaskAsync(Guid subtaskId, Guid taskId);
        Task<bool> SubtaskExistsAsync(Guid subtaskId);
    }
}