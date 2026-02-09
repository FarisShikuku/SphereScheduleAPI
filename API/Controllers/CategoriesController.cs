using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.Mappings;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SphereScheduleAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoriesController(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            try
            {
                var userId = GetCurrentUserId();
                var categories = await _categoryService.GetUserCategoriesAsync(userId);

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

                // Enhance with task counts
                foreach (var categoryDto in categoryDtos)
                {
                    var category = categories.FirstOrDefault(c => c.CategoryId == categoryDto.CategoryId);
                    if (category != null)
                    {
                        categoryDto.TaskCount = category.Tasks.Count;
                        categoryDto.ActiveTaskCount = category.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");
                    }
                }

                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving categories", error = ex.Message });
            }
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (category.UserId != userId)
                    return Forbid();

                var categoryDto = _mapper.Map<CategoryDto>(category);
                categoryDto.TaskCount = category.Tasks.Count;
                categoryDto.ActiveTaskCount = category.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving category", error = ex.Message });
            }
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Map DTO to entity
                var category = _mapper.Map<Category>(createDto);
                category.UserId = userId;

                var created = await _categoryService.CreateCategoryAsync(category);
                var categoryDto = _mapper.Map<CategoryDto>(created);

                return CreatedAtAction(nameof(GetCategory),
                    new { id = created.CategoryId },
                    categoryDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating category", error = ex.Message });
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateDto)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                // Map updates
                _mapper.Map(updateDto, existing);
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                var updated = await _categoryService.UpdateCategoryAsync(existing);
                var categoryDto = _mapper.Map<CategoryDto>(updated);
                categoryDto.TaskCount = updated.Tasks.Count;
                categoryDto.ActiveTaskCount = updated.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");

                return Ok(categoryDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category", error = ex.Message });
            }
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                // Check if category can be deleted
                if (!await _categoryService.CanDeleteCategoryAsync(id))
                    return BadRequest(new { message = "Cannot delete category with associated tasks" });

                var success = await _categoryService.DeleteCategoryAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete category" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting category", error = ex.Message });
            }
        }

        // GET: api/categories/type/{type}
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategoriesByType(string type)
        {
            try
            {
                var userId = GetCurrentUserId();
                var categories = await _categoryService.GetCategoriesByTypeAsync(userId, type);

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

                // Enhance with task counts
                foreach (var categoryDto in categoryDtos)
                {
                    var category = categories.FirstOrDefault(c => c.CategoryId == categoryDto.CategoryId);
                    if (category != null)
                    {
                        categoryDto.TaskCount = category.Tasks.Count;
                        categoryDto.ActiveTaskCount = category.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");
                    }
                }

                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving categories by type", error = ex.Message });
            }
        }

        // GET: api/categories/default
        [HttpGet("default")]
        public async Task<ActionResult<CategoryDto>> GetDefaultCategory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var category = await _categoryService.GetDefaultCategoryAsync(userId);

                if (category == null)
                    return NotFound(new { message = "No default category found" });

                var categoryDto = _mapper.Map<CategoryDto>(category);
                categoryDto.TaskCount = category.Tasks.Count;
                categoryDto.ActiveTaskCount = category.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving default category", error = ex.Message });
            }
        }

        // POST: api/categories/{id}/set-default
        [HttpPost("{id}/set-default")]
        public async Task<ActionResult> SetDefaultCategory(Guid id)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _categoryService.SetDefaultCategoryAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to set default category" });

                return Ok(new { message = "Default category updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error setting default category", error = ex.Message });
            }
        }

        // POST: api/categories/{id}/order
        [HttpPost("{id}/order")]
        public async Task<ActionResult> UpdateCategoryOrder(Guid id, [FromBody] UpdateCategoryOrderDto orderDto)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _categoryService.UpdateCategoryOrderAsync(id, orderDto.NewOrder);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update category order" });

                return Ok(new { message = "Category order updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category order", error = ex.Message });
            }
        }

        // POST: api/categories/reorder
        [HttpPost("reorder")]
        public async Task<ActionResult> ReorderCategories([FromBody] ReorderCategoriesDto reorderDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _categoryService.ReorderCategoriesAsync(userId, reorderDto.CategoryOrders);

                if (!success)
                    return StatusCode(500, new { message = "Failed to reorder categories" });

                return Ok(new { message = "Categories reordered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reordering categories", error = ex.Message });
            }
        }

        // POST: api/categories/{id}/color
        [HttpPost("{id}/color")]
        public async Task<ActionResult> UpdateCategoryColor(Guid id, [FromBody] UpdateCategoryColorDto colorDto)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _categoryService.UpdateCategoryColorAsync(id, colorDto.ColorCode);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update category color" });

                return Ok(new { message = "Category color updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category color", error = ex.Message });
            }
        }

        // POST: api/categories/{id}/icon
        [HttpPost("{id}/icon")]
        public async Task<ActionResult> UpdateCategoryIcon(Guid id, [FromBody] UpdateCategoryIconDto iconDto)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Category with ID {id} not found" });

                // Check ownership
                var userId = GetCurrentUserId();
                if (existing.UserId != userId)
                    return Forbid();

                var success = await _categoryService.UpdateCategoryIconAsync(id, iconDto.IconName);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update category icon" });

                return Ok(new { message = "Category icon updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category icon", error = ex.Message });
            }
        }

        // GET: api/categories/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<CategoryStatisticsDto>> GetCategoryStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();

                var categories = await _categoryService.GetUserCategoriesAsync(userId);
                var usageStats = await _categoryService.GetCategoryUsageStatisticsAsync(userId);
                var mostUsedCategory = await _categoryService.GetMostUsedCategoryAsync(userId);

                var statistics = new CategoryStatisticsDto
                {
                    TotalCategories = categories.Count(),
                    SystemCategories = categories.Count(c => c.CategoryType == "system"),
                    CustomCategories = categories.Count(c => c.CategoryType == "custom"),
                    CategoriesWithTasks = categories.Count(c => c.Tasks.Any()),
                    EmptyCategories = categories.Count(c => !c.Tasks.Any()),
                    CategoryUsage = usageStats,
                    MostUsedCategory = mostUsedCategory?.CategoryName,
                    MostUsedCategoryCount = mostUsedCategory?.Tasks.Count ?? 0
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving category statistics", error = ex.Message });
            }
        }

        // GET: api/categories/with-task-count
        [HttpGet("with-task-count")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategoriesWithTaskCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var categories = await _categoryService.GetCategoriesWithTaskCountAsync(userId);

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

                foreach (var categoryDto in categoryDtos)
                {
                    var category = categories.FirstOrDefault(c => c.CategoryId == categoryDto.CategoryId);
                    if (category != null)
                    {
                        categoryDto.TaskCount = category.Tasks.Count;
                        categoryDto.ActiveTaskCount = category.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");
                    }
                }

                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving categories with task count", error = ex.Message });
            }
        }

        // GET: api/categories/unused
        [HttpGet("unused")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetUnusedCategories([FromQuery] int days = 30)
        {
            try
            {
                var userId = GetCurrentUserId();
                var categories = await _categoryService.GetUnusedCategoriesAsync(userId, days);

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

                foreach (var categoryDto in categoryDtos)
                {
                    var category = categories.FirstOrDefault(c => c.CategoryId == categoryDto.CategoryId);
                    if (category != null)
                    {
                        categoryDto.TaskCount = category.Tasks.Count;
                        categoryDto.ActiveTaskCount = category.Tasks.Count(t => !t.IsDeleted && t.Status != "completed" && t.Status != "cancelled");
                    }
                }

                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving unused categories", error = ex.Message });
            }
        }

        // POST: api/categories/initialize-defaults
        [HttpPost("initialize-defaults")]
        public async Task<ActionResult> InitializeDefaultCategories()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _categoryService.InitializeDefaultCategoriesAsync(userId);

                if (!success)
                    return Conflict(new { message = "User already has categories or initialization failed" });

                return Ok(new { message = "Default categories initialized successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error initializing default categories", error = ex.Message });
            }
        }

        // POST: api/categories/reset-to-defaults
        [HttpPost("reset-to-defaults")]
        public async Task<ActionResult> ResetToDefaultCategories()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _categoryService.ResetToDefaultCategoriesAsync(userId);

                if (!success)
                    return BadRequest(new { message = "Cannot reset categories with tasks in custom categories" });

                return Ok(new { message = "Categories reset to defaults successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error resetting to default categories", error = ex.Message });
            }
        }

        // GET: api/categories/check-name
        [HttpGet("check-name")]
        public async Task<ActionResult> CheckCategoryName([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var exists = await _categoryService.CategoryNameExistsAsync(userId, name, excludeId);

                return Ok(new { name, exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking category name", error = ex.Message });
            }
        }

        // POST: api/categories/bulk/delete
        [HttpPost("bulk/delete")]
        public async Task<ActionResult> DeleteMultipleCategories([FromBody] BulkCategoryActionDto bulkDto)
        {
            try
            {
                if (bulkDto.CategoryIds == null || bulkDto.CategoryIds.Length == 0)
                    return BadRequest(new { message = "No category IDs provided" });

                var userId = GetCurrentUserId();

                // Verify all categories belong to user
                var categories = await _categoryService.GetUserCategoriesAsync(userId);
                var userCategoryIds = categories.Select(c => c.CategoryId).ToHashSet();

                if (bulkDto.CategoryIds.Any(id => !userCategoryIds.Contains(id)))
                    return Forbid();

                var success = await _categoryService.DeleteMultipleCategoriesAsync(bulkDto.CategoryIds);
                if (!success)
                    return BadRequest(new { message = "Cannot delete categories with associated tasks" });

                return Ok(new { message = "Categories deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting categories", error = ex.Message });
            }
        }

        // GET: api/categories/system
        [HttpGet("system")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetSystemCategories()
        {
            try
            {
                var categories = await _categoryService.GetSystemCategoriesAsync();
                return Ok(_mapper.Map<IEnumerable<CategoryDto>>(categories));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving system categories", error = ex.Message });
            }
        }
    }

    // Supporting DTO classes for this controller
    public class UpdateCategoryOrderDto
    {
        [Range(0, 1000)]
        public int NewOrder { get; set; }
    }

    public class BulkCategoryActionDto
    {
        [Required]
        public Guid[] CategoryIds { get; set; }
    }
}