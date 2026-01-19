using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Extensions;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Controllers;

/// <summary>
/// Controller for task CRUD operations with filtering and status updates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Gets the current authenticated user's ID safely with proper validation
    /// </summary>
    private int UserId => User.GetUserIdOrThrow();

    /// <summary>
    /// Validates that a category belongs to the current user
    /// </summary>
    private async Task<bool> IsCategoryOwnedByUser(int? categoryId)
    {
        if (!categoryId.HasValue) return true;
        return await _taskService.ValidateCategoryOwnershipAsync(UserId, categoryId.Value);
    }

    /// <summary>
    /// Get all tasks for the current user with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TaskResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TaskResponse>>>> GetTasks([FromQuery] TaskFilterParams filters)
    {
        var tasks = await _taskService.GetTasksAsync(UserId, filters);
        return Ok(ApiResponse<List<TaskResponse>>.Ok(tasks));
    }

    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetTask(int id)
    {
        var task = await _taskService.GetTaskAsync(UserId, id);

        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }

        return Ok(ApiResponse<TaskResponse>.Ok(task));
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (!await IsCategoryOwnedByUser(request.CategoryId))
        {
            return BadRequest(ApiResponse.Fail("Invalid category"));
        }

        var task = await _taskService.CreateTaskAsync(UserId, request);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<TaskResponse>.Ok(task, "Task created successfully"));
    }

    /// <summary>
    /// Update an existing task
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        if (!await IsCategoryOwnedByUser(request.CategoryId))
        {
            return BadRequest(ApiResponse.Fail("Invalid category"));
        }

        var task = await _taskService.UpdateTaskAsync(UserId, id, request);

        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }

        return Ok(ApiResponse<TaskResponse>.Ok(task, "Task updated successfully"));
    }

    /// <summary>
    /// Update only the status of a task (for drag-and-drop operations)
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _taskService.UpdateTaskStatusAsync(UserId, id, request.Status);

        if (task == null)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }

        return Ok(ApiResponse<TaskResponse>.Ok(task, "Task status updated"));
    }

    /// <summary>
    /// Delete a task
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTask(int id)
    {
        var deleted = await _taskService.DeleteTaskAsync(UserId, id);

        if (!deleted)
        {
            return NotFound(ApiResponse.Fail("Task not found"));
        }

        return Ok(ApiResponse.Ok("Task deleted successfully"));
    }

    /// <summary>
    /// Get task statistics for the current user
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<TaskStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TaskStatsResponse>>> GetTaskStats()
    {
        var stats = await _taskService.GetTaskStatsAsync(UserId);
        return Ok(ApiResponse<TaskStatsResponse>.Ok(stats));
    }
}
