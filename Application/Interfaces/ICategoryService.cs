using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface ICategoryService
    {
        // Basic CRUD
        Task<Category> GetCategoryByIdAsync(Guid CategoryID);
        Task<IEnumerable<Category>> GetUserCategoriesAsync(Guid UserID);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(Guid CategoryID);
        Task<bool> RestoreCategoryAsync(Guid CategoryID);

        // Query operations
        Task<IEnumerable<Category>> GetCategoriesByTypeAsync(Guid UserID, string categoryType);
        Task<Category> GetDefaultCategoryAsync(Guid UserID);
        Task<IEnumerable<Category>> GetCategoriesWithTaskCountAsync(Guid UserID);
        Task<Category> GetCategoryByNameAsync(Guid UserID, string categoryName);

        // Category management
        Task<bool> SetDefaultCategoryAsync(Guid CategoryID);
        Task<bool> UpdateCategoryOrderAsync(Guid CategoryID, int newOrder);
        Task<bool> ReorderCategoriesAsync(Guid UserID, Dictionary<Guid, int> categoryOrders);
        Task<bool> UpdateCategoryColorAsync(Guid CategoryID, string colorCode);
        Task<bool> UpdateCategoryIconAsync(Guid CategoryID, string iconName);

        // Statistics
        Task<int> GetCategoryCountByUserAsync(Guid UserID);
        Task<Dictionary<string, int>> GetCategoryUsageStatisticsAsync(Guid UserID);
        Task<Category> GetMostUsedCategoryAsync(Guid UserID);
        Task<IEnumerable<Category>> GetUnusedCategoriesAsync(Guid UserID, int daysThreshold = 30);

        // Bulk operations
        Task<bool> DeleteMultipleCategoriesAsync(Guid[] CategoryIDs);
        Task<bool> UpdateMultipleCategoriesAsync(Guid[] CategoryIDs, Action<Category> updateAction);

        // System categories
        Task<IEnumerable<Category>> GetSystemCategoriesAsync();
        Task<bool> InitializeDefaultCategoriesAsync(Guid UserID);
        Task<bool> ResetToDefaultCategoriesAsync(Guid UserID);

        // Validation
        Task<bool> CategoryNameExistsAsync(Guid UserID, string categoryName, Guid? excludeCategoryID = null);
        Task<bool> CanDeleteCategoryAsync(Guid CategoryID); // Check if category has associated tasks
    }
}