using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Extensions;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Controllers;

/// <summary>
/// Controller for category CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Gets the current authenticated user's ID safely with proper validation
    /// </summary>
    private int UserId => User.GetUserIdOrThrow();

    /// <summary>
    /// Checks if a category name already exists for the user
    /// </summary>
    private async Task<bool> IsCategoryNameTaken(string name, int? excludeId = null)
        => await _categoryService.CategoryNameExistsAsync(UserId, name, excludeId);

    /// <summary>
    /// Get all categories for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetCategories()
    {
        var categories = await _categoryService.GetCategoriesAsync(UserId);
        return Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }

    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetCategory(int id)
    {
        var category = await _categoryService.GetCategoryAsync(UserId, id);

        if (category == null)
        {
            return NotFound(ApiResponse.Fail("Category not found"));
        }

        return Ok(ApiResponse<CategoryResponse>.Ok(category));
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (await IsCategoryNameTaken(request.Name))
        {
            return BadRequest(ApiResponse.Fail("A category with this name already exists"));
        }

        var category = await _categoryService.CreateCategoryAsync(UserId, request);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CategoryResponse>.Ok(category, "Category created successfully"));
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
        if (await IsCategoryNameTaken(request.Name, id))
        {
            return BadRequest(ApiResponse.Fail("A category with this name already exists"));
        }

        var category = await _categoryService.UpdateCategoryAsync(UserId, id, request);

        if (category == null)
        {
            return NotFound(ApiResponse.Fail("Category not found"));
        }

        return Ok(ApiResponse<CategoryResponse>.Ok(category, "Category updated successfully"));
    }

    /// <summary>
    /// Delete a category (tasks will have their category set to null)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(int id)
    {
        var deleted = await _categoryService.DeleteCategoryAsync(UserId, id);

        if (!deleted)
        {
            return NotFound(ApiResponse.Fail("Category not found"));
        }

        return Ok(ApiResponse.Ok("Category deleted successfully"));
    }
}
