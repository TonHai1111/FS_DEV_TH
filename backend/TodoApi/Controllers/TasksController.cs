using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Controllers;

/// <summary>
/// Controller for task CRUD operations with filtering and status updates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public TasksController(AppDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    /// <summary>
    /// Get all tasks for the current user with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TaskResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TaskResponse>>>> GetTasks([FromQuery] TaskFilterParams filters)
    {
        var query = _context.Tasks
            .Where(t => t.UserId == UserId)
            .Include(t => t.Category)
            .AsQueryable();
        
        // Apply filters
        if (filters.Status.HasValue)
        {
            query = query.Where(t => t.Status == filters.Status.Value);
        }
        
        if (filters.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filters.Priority.Value);
        }
        
        if (filters.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filters.CategoryId.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var searchLower = filters.Search.ToLower();
            query = query.Where(t => 
                t.Title.ToLower().Contains(searchLower) || 
                (t.Description != null && t.Description.ToLower().Contains(searchLower)));
        }
        
        if (filters.Overdue == true)
        {
            query = query.Where(t => 
                t.DueDate.HasValue && 
                t.DueDate.Value < DateTime.UtcNow && 
                t.Status != Models.TaskStatus.Done);
        }
        
        // Apply sorting
        query = filters.SortBy?.ToLower() switch
        {
            "duedate" => filters.SortDescending 
                ? query.OrderByDescending(t => t.DueDate) 
                : query.OrderBy(t => t.DueDate),
            "priority" => filters.SortDescending 
                ? query.OrderByDescending(t => t.Priority) 
                : query.OrderBy(t => t.Priority),
            "title" => filters.SortDescending 
                ? query.OrderByDescending(t => t.Title) 
                : query.OrderBy(t => t.Title),
            "status" => filters.SortDescending 
                ? query.OrderByDescending(t => t.Status) 
                : query.OrderBy(t => t.Status),
            _ => filters.SortDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt)
        };
        
        var tasks = await query.ToListAsync();
        var response = tasks.Select(MapToTaskResponse).ToList();
        
        return Ok(ApiResponse<List<TaskResponse>>.Ok(response));
    }
    
    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetTask(int id)
    {
        var task = await _context.Tasks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        
        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }
        
        return Ok(ApiResponse<TaskResponse>.Ok(MapToTaskResponse(task)));
    }
    
    /// <summary>
    /// Create a new task
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> CreateTask([FromBody] CreateTaskRequest request)
    {
        // Validate category belongs to user if provided
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == UserId);
            
            if (!categoryExists)
            {
                return BadRequest(ApiResponse.Fail("Invalid category"));
            }
        }
        
        var task = new TodoTask
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CategoryId = request.CategoryId,
            UserId = UserId,
            Status = Models.TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        // Reload with category
        await _context.Entry(task).Reference(t => t.Category).LoadAsync();
        
        return StatusCode(StatusCodes.Status201Created, 
            ApiResponse<TaskResponse>.Ok(MapToTaskResponse(task), "Task created successfully"));
    }
    
    /// <summary>
    /// Update an existing task
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        
        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }
        
        // Validate category belongs to user if provided
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == UserId);
            
            if (!categoryExists)
            {
                return BadRequest(ApiResponse.Fail("Invalid category"));
            }
        }
        
        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.CategoryId = request.CategoryId;
        
        await _context.SaveChangesAsync();
        
        // Reload with category
        await _context.Entry(task).Reference(t => t.Category).LoadAsync();
        
        return Ok(ApiResponse<TaskResponse>.Ok(MapToTaskResponse(task), "Task updated successfully"));
    }
    
    /// <summary>
    /// Update only the status of a task (for drag-and-drop operations)
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _context.Tasks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        
        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }
        
        task.Status = request.Status;
        await _context.SaveChangesAsync();
        
        return Ok(ApiResponse<TaskResponse>.Ok(MapToTaskResponse(task), "Task status updated"));
    }
    
    /// <summary>
    /// Delete a task
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTask(int id)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        
        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }
        
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        
        return Ok(ApiResponse.Ok("Task deleted successfully"));
    }
    
    /// <summary>
    /// Get task statistics for the current user
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<TaskStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TaskStatsResponse>>> GetTaskStats()
    {
        var tasks = await _context.Tasks
            .Where(t => t.UserId == UserId)
            .ToListAsync();
        
        var stats = new TaskStatsResponse
        {
            Total = tasks.Count,
            Todo = tasks.Count(t => t.Status == Models.TaskStatus.Todo),
            InProgress = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
            Done = tasks.Count(t => t.Status == Models.TaskStatus.Done),
            Overdue = tasks.Count(t => 
                t.DueDate.HasValue && 
                t.DueDate.Value < DateTime.UtcNow && 
                t.Status != Models.TaskStatus.Done)
        };
        
        return Ok(ApiResponse<TaskStatsResponse>.Ok(stats));
    }
    
    /// <summary>
    /// Maps a TodoTask entity to a TaskResponse DTO
    /// </summary>
    private static TaskResponse MapToTaskResponse(TodoTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CategoryId = task.CategoryId,
            Category = task.Category != null ? new CategoryResponse
            {
                Id = task.Category.Id,
                Name = task.Category.Name,
                Color = task.Category.Color
            } : null
        };
    }
}

/// <summary>
/// Response DTO for task statistics
/// </summary>
public class TaskStatsResponse
{
    public int Total { get; set; }
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
}
