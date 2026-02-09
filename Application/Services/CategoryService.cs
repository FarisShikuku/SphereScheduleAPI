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
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Category> GetCategoryByIdAsync(Guid categoryId)
        {
            return await _context.Categories
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId && !c.IsDeleted);
        }

        public async Task<IEnumerable<Category>> GetUserCategoriesAsync(Guid userId)
        {
            return await _context.Categories
                .Include(c => c.Tasks)
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderBy(c => c.CategoryOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            // Validate unique category name for user
            if (await CategoryNameExistsAsync(category.UserId, category.CategoryName))
                throw new InvalidOperationException($"Category '{category.CategoryName}' already exists for this user");

            category.CategoryId = Guid.NewGuid();
            category.CreatedAt = DateTimeOffset.UtcNow;
            category.UpdatedAt = DateTimeOffset.UtcNow;
            category.IsDeleted = false;

            // Set order to last if not specified
            if (category.CategoryOrder == 0)
            {
                var maxOrder = await _context.Categories
                    .Where(c => c.UserId == category.UserId && !c.IsDeleted)
                    .MaxAsync(c => (int?)c.CategoryOrder) ?? 0;
                category.CategoryOrder = maxOrder + 1;
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            var existing = await GetCategoryByIdAsync(category.CategoryId);
            if (existing == null)
                throw new KeyNotFoundException($"Category with ID {category.CategoryId} not found");

            // Validate unique category name if changing
            if (existing.CategoryName != category.CategoryName &&
                await CategoryNameExistsAsync(existing.UserId, category.CategoryName, category.CategoryId))
                throw new InvalidOperationException($"Category '{category.CategoryName}' already exists for this user");

            category.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(Guid categoryId)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            // Check if category has associated tasks
            if (category.Tasks.Any() && category.CategoryType != "system")
                return false; // Can't delete categories with tasks (except system categories)

            // For system categories, just mark as deleted but don't actually delete
            if (category.CategoryType == "system")
            {
                category.IsDeleted = true;
                category.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreCategoryAsync(Guid categoryId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.IsDeleted);

            if (category == null) return false;

            category.IsDeleted = false;
            category.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Category>> GetCategoriesByTypeAsync(Guid userId, string categoryType)
        {
            return await _context.Categories
                .Where(c => c.UserId == userId &&
                           !c.IsDeleted &&
                           c.CategoryType == categoryType)
                .OrderBy(c => c.CategoryOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Category> GetDefaultCategoryAsync(Guid userId)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                        !c.IsDeleted &&
                                        c.IsDefault);
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithTaskCountAsync(Guid userId)
        {
            return await _context.Categories
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => new Category
                {
                    CategoryId = c.CategoryId,
                    UserId = c.UserId,
                    CategoryName = c.CategoryName,
                    CategoryType = c.CategoryType,
                    Description = c.Description,
                    ColorCode = c.ColorCode,
                    IconName = c.IconName,
                    CategoryOrder = c.CategoryOrder,
                    IsDefault = c.IsDefault,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Tasks = c.Tasks.Where(t => !t.IsDeleted).ToList()
                })
                .OrderBy(c => c.CategoryOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Category> GetCategoryByNameAsync(Guid userId, string categoryName)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                        !c.IsDeleted &&
                                        c.CategoryName == categoryName);
        }

        public async Task<bool> SetDefaultCategoryAsync(Guid categoryId)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            // Remove default from all other user categories
            var userCategories = await _context.Categories
                .Where(c => c.UserId == category.UserId && !c.IsDeleted)
                .ToListAsync();

            foreach (var cat in userCategories)
            {
                cat.IsDefault = (cat.CategoryId == categoryId);
                cat.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryOrderAsync(Guid categoryId, int newOrder)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            // Get all categories for this user
            var categories = await _context.Categories
                .Where(c => c.UserId == category.UserId && !c.IsDeleted)
                .OrderBy(c => c.CategoryOrder)
                .ToListAsync();

            // Remove category from list and insert at new position
            var categoryToMove = categories.FirstOrDefault(c => c.CategoryId == categoryId);
            if (categoryToMove == null) return false;

            categories.Remove(categoryToMove);
            newOrder = Math.Max(0, Math.Min(newOrder, categories.Count));
            categories.Insert(newOrder, categoryToMove);

            // Update orders
            for (int i = 0; i < categories.Count; i++)
            {
                categories[i].CategoryOrder = i;
                categories[i].UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderCategoriesAsync(Guid userId, Dictionary<Guid, int> categoryOrders)
        {
            var categories = await _context.Categories
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .ToListAsync();

            foreach (var category in categories)
            {
                if (categoryOrders.TryGetValue(category.CategoryId, out int newOrder))
                {
                    category.CategoryOrder = newOrder;
                    category.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryColorAsync(Guid categoryId, string colorCode)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            category.ColorCode = colorCode;
            category.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryIconAsync(Guid categoryId, string iconName)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            category.IconName = iconName;
            category.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCategoryCountByUserAsync(Guid userId)
        {
            return await _context.Categories
                .CountAsync(c => c.UserId == userId && !c.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetCategoryUsageStatisticsAsync(Guid userId)
        {
            var categories = await _context.Categories
                .Include(c => c.Tasks)
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .ToListAsync();

            var stats = new Dictionary<string, int>();

            foreach (var category in categories)
            {
                var taskCount = category.Tasks.Count(t => !t.IsDeleted);
                stats.Add(category.CategoryName, taskCount);
            }

            return stats;
        }

        public async Task<Category> GetMostUsedCategoryAsync(Guid userId)
        {
            var categories = await _context.Categories
                .Include(c => c.Tasks)
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .ToListAsync();

            return categories
                .OrderByDescending(c => c.Tasks.Count(t => !t.IsDeleted))
                .FirstOrDefault();
        }

        public async Task<IEnumerable<Category>> GetUnusedCategoriesAsync(Guid userId, int daysThreshold = 30)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysThreshold);

            return await _context.Categories
                .Include(c => c.Tasks)
                .Where(c => c.UserId == userId &&
                           !c.IsDeleted &&
                           !c.Tasks.Any(t => !t.IsDeleted && t.CreatedAt >= cutoffDate))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<bool> DeleteMultipleCategoriesAsync(Guid[] categoryIds)
        {
            var categories = await _context.Categories
                .Include(c => c.Tasks)
                .Where(c => categoryIds.Contains(c.CategoryId) && !c.IsDeleted)
                .ToListAsync();

            // Check if any category has associated tasks
            if (categories.Any(c => c.Tasks.Any() && c.CategoryType != "system"))
                return false;

            foreach (var category in categories)
            {
                if (category.CategoryType == "system")
                {
                    category.IsDeleted = true;
                    category.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    _context.Categories.Remove(category);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateMultipleCategoriesAsync(Guid[] categoryIds, Action<Category> updateAction)
        {
            var categories = await _context.Categories
                .Where(c => categoryIds.Contains(c.CategoryId) && !c.IsDeleted)
                .ToListAsync();

            foreach (var category in categories)
            {
                updateAction(category);
                category.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Category>> GetSystemCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.CategoryType == "system" && !c.IsDeleted)
                .OrderBy(c => c.CategoryOrder)
                .ToListAsync();
        }

        public async Task<bool> InitializeDefaultCategoriesAsync(Guid userId)
        {
            // Check if user already has categories
            var existingCategories = await GetUserCategoriesAsync(userId);
            if (existingCategories.Any())
                return false;

            var defaultCategories = new List<Category>
            {
                new Category
                {
                    UserId = userId,
                    CategoryName = "Work",
                    CategoryType = "system",
                    Description = "Work-related tasks",
                    ColorCode = "#2196F3",
                    IconName = "work",
                    CategoryOrder = 1,
                    IsDefault = true
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Personal",
                    CategoryType = "system",
                    Description = "Personal tasks",
                    ColorCode = "#4CAF50",
                    IconName = "person",
                    CategoryOrder = 2
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Health",
                    CategoryType = "system",
                    Description = "Health and fitness",
                    ColorCode = "#F44336",
                    IconName = "health",
                    CategoryOrder = 3
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Education",
                    CategoryType = "system",
                    Description = "Learning and education",
                    ColorCode = "#9C27B0",
                    IconName = "school",
                    CategoryOrder = 4
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Shopping",
                    CategoryType = "system",
                    Description = "Shopping lists",
                    ColorCode = "#FF9800",
                    IconName = "shopping",
                    CategoryOrder = 5
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Finance",
                    CategoryType = "system",
                    Description = "Financial tasks",
                    ColorCode = "#795548",
                    IconName = "finance",
                    CategoryOrder = 6
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Entertainment",
                    CategoryType = "system",
                    Description = "Entertainment and leisure",
                    ColorCode = "#E91E63",
                    IconName = "entertainment",
                    CategoryOrder = 7
                },
                new Category
                {
                    UserId = userId,
                    CategoryName = "Other",
                    CategoryType = "system",
                    Description = "Other tasks",
                    ColorCode = "#9E9E9E",
                    IconName = "help",
                    CategoryOrder = 8
                }
            };

            foreach (var category in defaultCategories)
            {
                category.CategoryId = Guid.NewGuid();
                category.CreatedAt = DateTimeOffset.UtcNow;
                category.UpdatedAt = DateTimeOffset.UtcNow;
                _context.Categories.Add(category);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetToDefaultCategoriesAsync(Guid userId)
        {
            // Delete all user's custom categories
            var customCategories = await _context.Categories
                .Where(c => c.UserId == userId &&
                           c.CategoryType == "custom" &&
                           !c.IsDeleted)
                .ToListAsync();

            // Check if any custom category has associated tasks
            if (customCategories.Any(c => c.Tasks.Any()))
                return false;

            foreach (var category in customCategories)
            {
                category.IsDeleted = true;
                category.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Initialize default categories if none exist
            var existingCategories = await GetUserCategoriesAsync(userId);
            if (!existingCategories.Any())
                await InitializeDefaultCategoriesAsync(userId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CategoryNameExistsAsync(Guid userId, string categoryName, Guid? excludeCategoryId = null)
        {
            var query = _context.Categories
                .Where(c => c.UserId == userId &&
                           !c.IsDeleted &&
                           c.CategoryName == categoryName);

            if (excludeCategoryId.HasValue)
                query = query.Where(c => c.CategoryId != excludeCategoryId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> CanDeleteCategoryAsync(Guid categoryId)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            // Can delete if no tasks or it's a system category
            return !category.Tasks.Any() || category.CategoryType == "system";
        }
    }
}