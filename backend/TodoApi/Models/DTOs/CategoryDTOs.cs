using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models.DTOs;

/// <summary>
/// Request DTO for creating a new category
/// </summary>
public class CreateCategoryRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Hex color code (e.g., "#FF5733"). Defaults to indigo if not provided.
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string Color { get; set; } = "#6366F1";
}

/// <summary>
/// Request DTO for updating an existing category
/// </summary>
public class UpdateCategoryRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string Color { get; set; } = "#6366F1";
}

/// <summary>
/// Response DTO for category information
/// </summary>
public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    
    /// <summary>
    /// Count of tasks in this category
    /// </summary>
    public int TaskCount { get; set; }
}
