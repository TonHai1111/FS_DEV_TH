using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Services;

/// <summary>
/// Service implementation for category operations
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Maps a Category entity to a CategoryResponse DTO
    /// </summary>
    private static CategoryResponse ToCategoryResponse(Category category)
        => new()
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            TaskCount = category.Tasks?.Count ?? 0
        };

    /// <inheritdoc />
    public async Task<List<CategoryResponse>> GetCategoriesAsync(int userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Tasks)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(ToCategoryResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<CategoryResponse?> GetCategoryAsync(int userId, int categoryId)
    {
        var category = await _context.Categories
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        return category == null ? null : ToCategoryResponse(category);
    }

    /// <inheritdoc />
    public async Task<CategoryResponse> CreateCategoryAsync(int userId, CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Color = request.Color,
            UserId = userId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return ToCategoryResponse(category);
    }

    /// <inheritdoc />
    public async Task<CategoryResponse?> UpdateCategoryAsync(int userId, int categoryId, UpdateCategoryRequest request)
    {
        var category = await _context.Categories
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
        {
            return null;
        }

        category.Name = request.Name;
        category.Color = request.Color;

        await _context.SaveChangesAsync();

        return ToCategoryResponse(category);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategoryAsync(int userId, int categoryId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
        {
            return false;
        }

        // Explicitly clear CategoryId for all tasks in this category
        // This ensures proper cascade handling and data consistency
        var tasksInCategory = await _context.Tasks
            .Where(t => t.CategoryId == categoryId && t.UserId == userId)
            .ToListAsync();

        foreach (var task in tasksInCategory)
        {
            task.CategoryId = null;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CategoryNameExistsAsync(int userId, string name, int? excludeCategoryId = null)
    {
        var query = _context.Categories
            .Where(c => c.UserId == userId && c.Name.ToLower() == name.ToLower());

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }

        return await query.AnyAsync();
    }
}
