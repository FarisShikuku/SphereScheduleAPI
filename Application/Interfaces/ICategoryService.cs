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
        Task<Category> GetCategoryByIdAsync(Guid categoryId);
        Task<IEnumerable<Category>> GetUserCategoriesAsync(Guid userId);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(Guid categoryId);
        Task<bool> RestoreCategoryAsync(Guid categoryId);

        // Query operations
        Task<IEnumerable<Category>> GetCategoriesByTypeAsync(Guid userId, string categoryType);
        Task<Category> GetDefaultCategoryAsync(Guid userId);
        Task<IEnumerable<Category>> GetCategoriesWithTaskCountAsync(Guid userId);
        Task<Category> GetCategoryByNameAsync(Guid userId, string categoryName);

        // Category management
        Task<bool> SetDefaultCategoryAsync(Guid categoryId);
        Task<bool> UpdateCategoryOrderAsync(Guid categoryId, int newOrder);
        Task<bool> ReorderCategoriesAsync(Guid userId, Dictionary<Guid, int> categoryOrders);
        Task<bool> UpdateCategoryColorAsync(Guid categoryId, string colorCode);
        Task<bool> UpdateCategoryIconAsync(Guid categoryId, string iconName);

        // Statistics
        Task<int> GetCategoryCountByUserAsync(Guid userId);
        Task<Dictionary<string, int>> GetCategoryUsageStatisticsAsync(Guid userId);
        Task<Category> GetMostUsedCategoryAsync(Guid userId);
        Task<IEnumerable<Category>> GetUnusedCategoriesAsync(Guid userId, int daysThreshold = 30);

        // Bulk operations
        Task<bool> DeleteMultipleCategoriesAsync(Guid[] categoryIds);
        Task<bool> UpdateMultipleCategoriesAsync(Guid[] categoryIds, Action<Category> updateAction);

        // System categories
        Task<IEnumerable<Category>> GetSystemCategoriesAsync();
        Task<bool> InitializeDefaultCategoriesAsync(Guid userId);
        Task<bool> ResetToDefaultCategoriesAsync(Guid userId);

        // Validation
        Task<bool> CategoryNameExistsAsync(Guid userId, string categoryName, Guid? excludeCategoryId = null);
        Task<bool> CanDeleteCategoryAsync(Guid categoryId); // Check if category has associated tasks
    }
}