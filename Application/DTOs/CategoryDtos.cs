using System;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class CategoryDto
    {
        public Guid CategoryId { get; set; }
        public Guid UserId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public string Description { get; set; }
        public string ColorCode { get; set; }
        public string IconName { get; set; }
        public int CategoryOrder { get; set; }
        public bool IsDefault { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int TaskCount { get; set; }
        public int ActiveTaskCount { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CategoryName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string CategoryType { get; set; } = "custom";

        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string ColorCode { get; set; } = "#4CAF50";

        [StringLength(50)]
        public string IconName { get; set; }

        [Range(0, 1000)]
        public int CategoryOrder { get; set; } = 0;

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateCategoryDto
    {
        [StringLength(100, MinimumLength = 1)]
        public string CategoryName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string ColorCode { get; set; }

        [StringLength(50)]
        public string IconName { get; set; }

        [Range(0, 1000)]
        public int? CategoryOrder { get; set; }

        public bool? IsDefault { get; set; }
    }

    public class ReorderCategoriesDto
    {
        [Required]
        public Dictionary<Guid, int> CategoryOrders { get; set; }
    }

    public class CategoryStatisticsDto
    {
        public int TotalCategories { get; set; }
        public int SystemCategories { get; set; }
        public int CustomCategories { get; set; }
        public int CategoriesWithTasks { get; set; }
        public int EmptyCategories { get; set; }
        public Dictionary<string, int> CategoryUsage { get; set; }
        public string MostUsedCategory { get; set; }
        public int MostUsedCategoryCount { get; set; }
    }

    public class UpdateCategoryColorDto
    {
        [Required]
        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string ColorCode { get; set; }
    }

    public class UpdateCategoryIconDto
    {
        [Required]
        [StringLength(50)]
        public string IconName { get; set; }
    }
}