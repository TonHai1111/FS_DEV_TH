using TodoApi.Models.DTOs;

namespace TodoApi.Services;

/// <summary>
/// Service interface for category operations
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets all categories for a user
    /// </summary>
    Task<List<CategoryResponse>> GetCategoriesAsync(int userId);

    /// <summary>
    /// Gets a specific category by ID for a user
    /// </summary>
    Task<CategoryResponse?> GetCategoryAsync(int userId, int categoryId);

    /// <summary>
    /// Creates a new category for a user
    /// </summary>
    Task<CategoryResponse> CreateCategoryAsync(int userId, CreateCategoryRequest request);

    /// <summary>
    /// Updates an existing category
    /// </summary>
    Task<CategoryResponse?> UpdateCategoryAsync(int userId, int categoryId, UpdateCategoryRequest request);

    /// <summary>
    /// Deletes a category
    /// </summary>
    Task<bool> DeleteCategoryAsync(int userId, int categoryId);

    /// <summary>
    /// Checks if a category name already exists for a user
    /// </summary>
    Task<bool> CategoryNameExistsAsync(int userId, string name, int? excludeCategoryId = null);
}
