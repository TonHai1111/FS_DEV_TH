using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Controllers;

/// <summary>
/// Controller for category CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    /// <summary>
    /// Get all categories for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetCategories()
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == UserId)
            .Include(c => c.Tasks)
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        var response = categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color,
            TaskCount = c.Tasks.Count
        }).ToList();
        
        return Ok(ApiResponse<List<CategoryResponse>>.Ok(response));
    }
    
    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        
        if (category == null)
        {
            return NotFound(ApiResponse.Fail("Category not found"));
        }
        
        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            TaskCount = category.Tasks.Count
        };
        
        return Ok(ApiResponse<CategoryResponse>.Ok(response));
    }
    
    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        // Check for duplicate name
        var exists = await _context.Categories
            .AnyAsync(c => c.UserId == UserId && c.Name.ToLower() == request.Name.ToLower());
        
        if (exists)
        {
            return BadRequest(ApiResponse.Fail("A category with this name already exists"));
        }
        
        var category = new Category
        {
            Name = request.Name,
            Color = request.Color,
            UserId = UserId
        };
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            TaskCount = 0
        };
        
        return StatusCode(StatusCodes.Status201Created, 
            ApiResponse<CategoryResponse>.Ok(response, "Category created successfully"));
    }
    
    /// <summary>
    /// Update an existing category
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _context.Categories
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        
        if (category == null)
        {
            return NotFound(ApiResponse.Fail("Category not found"));
        }
        
        // Check for duplicate name (excluding current category)
        var duplicateExists = await _context.Categories
            .AnyAsync(c => c.UserId == UserId && 
                          c.Id != id && 
                          c.Name.ToLower() == request.Name.ToLower());
        
        if (duplicateExists)
        {
            return BadRequest(ApiResponse.Fail("A category with this name already exists"));
        }
        
        category.Name = request.Name;
        category.Color = request.Color;
        
        await _context.SaveChangesAsync();
        
        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            TaskCount = category.Tasks.Count
        };
        
        return Ok(ApiResponse<CategoryResponse>.Ok(response, "Category updated successfully"));
    }
    
    /// <summary>
    /// Delete a category (tasks will have their category set to null)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        
        if (category == null)
        {
            return NotFound(ApiResponse.Fail("Category not found"));
        }
        
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        
        return Ok(ApiResponse.Ok("Category deleted successfully"));
    }
}
