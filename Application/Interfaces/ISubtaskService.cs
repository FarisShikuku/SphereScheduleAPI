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
        Task<Subtask> GetSubtaskByIdAsync(Guid subTaskID);
        Task<IEnumerable<Subtask>> GetTaskSubtasksAsync(Guid TaskID);
        Task<Subtask> CreateSubtaskAsync(Subtask subtask);
        Task<Subtask> UpdateSubtaskAsync(Subtask subtask);
        Task<bool> DeleteSubtaskAsync(Guid subTaskID);
        Task<bool> RestoreSubtaskAsync(Guid subTaskID);

        // Query operations
        Task<IEnumerable<Subtask>> GetSubtasksByStatusAsync(Guid TaskID, string status);
        Task<IEnumerable<Subtask>> GetSubtasksByPriorityAsync(Guid TaskID, string priority);
        Task<IEnumerable<Subtask>> GetOverdueSubtasksAsync(Guid TaskID);
        Task<IEnumerable<Subtask>> GetTodaySubtasksAsync(Guid TaskID);
        Task<IEnumerable<Subtask>> GetUpcomingSubtasksAsync(Guid TaskID, int daysAhead = 7);
        Task<IEnumerable<Subtask>> GetCompletedSubtasksAsync(Guid TaskID, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<Subtask>> GetSubtasksByDateRangeAsync(Guid TaskID, DateTime startDate, DateTime endDate);

        // Subtask management
        Task<bool> ChangeSubtaskStatusAsync(Guid subTaskID, string newStatus);
        Task<bool> CompleteSubtaskAsync(Guid subTaskID);
        Task<bool> UpdateSubtaskProgressAsync(Guid subTaskID, string status);
        Task<bool> UpdateSubtaskPriorityAsync(Guid subTaskID, string newPriority);
        Task<bool> UpdateSubtaskOrderAsync(Guid subTaskID, int newOrder);
        Task<bool> ReorderSubtasksAsync(Guid TaskID, Dictionary<Guid, int> subtaskOrders);
        Task<bool> UpdateSubtaskDueDateAsync(Guid subTaskID, DateTime? dueDate, TimeSpan? dueTime = null);

        // Statistics
        Task<int> GetSubtaskCountAsync(Guid TaskID);
        Task<Dictionary<string, int>> GetSubtaskStatisticsAsync(Guid TaskID);
        Task<decimal> GetSubtaskCompletionRateAsync(Guid TaskID);
        Task<TimeSpan?> GetAverageCompletionTimeAsync(Guid TaskID);

        // Bulk operations
        Task<bool> CreateMultipleSubtasksAsync(Guid TaskID, IEnumerable<Subtask> subtasks);
        Task<bool> DeleteMultipleSubtasksAsync(Guid[] subTaskIDs);
        Task<bool> CompleteMultipleSubtasksAsync(Guid[] subTaskIDs);
        Task<bool> ChangeMultipleSubtasksStatusAsync(Guid[] subTaskIDs, string newStatus);
        Task<bool> ChangeMultipleSubtasksPriorityAsync(Guid[] subTaskIDs, string newPriority);

        // Task relationship
        Task<bool> MoveSubtaskToTaskAsync(Guid subTaskID, Guid newTaskID);
        Task<bool> CopySubtaskToTaskAsync(Guid subTaskID, Guid newTaskID);
        Task<IEnumerable<Subtask>> GetSubtasksWithParentInfoAsync(Guid TaskID);

        // Search and filter
        Task<IEnumerable<Subtask>> SearchSubtasksAsync(Guid TaskID, string searchTerm);
        Task<IEnumerable<Subtask>> FilterSubtasksAsync(Guid TaskID, SubtaskFilterDto filter);

        // Validation
        Task<bool> CanDeleteSubtaskAsync(Guid subTaskID);
        Task<bool> SubtaskBelongsToTaskAsync(Guid subTaskID, Guid TaskID);
        Task<bool> SubtaskExistsAsync(Guid subTaskID);
    }
}