using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Services;

/// <summary>
/// Service interface for task operations
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Gets all tasks for a user with optional filtering and pagination
    /// </summary>
    Task<List<TaskResponse>> GetTasksAsync(int userId, TaskFilterParams filters);

    /// <summary>
    /// Gets tasks with pagination metadata for a user with optional filtering
    /// </summary>
    Task<PagedTaskResponse> GetTasksPagedAsync(int userId, TaskFilterParams filters);

    /// <summary>
    /// Gets a specific task by ID for a user
    /// </summary>
    Task<TaskResponse?> GetTaskAsync(int userId, int taskId);

    /// <summary>
    /// Creates a new task for a user
    /// </summary>
    Task<TaskResponse> CreateTaskAsync(int userId, CreateTaskRequest request);

    /// <summary>
    /// Updates an existing task
    /// </summary>
    Task<TaskResponse?> UpdateTaskAsync(int userId, int taskId, UpdateTaskRequest request);

    /// <summary>
    /// Updates only the status of a task
    /// </summary>
    Task<TaskResponse?> UpdateTaskStatusAsync(int userId, int taskId, Models.TaskStatus status);

    /// <summary>
    /// Deletes a task
    /// </summary>
    Task<bool> DeleteTaskAsync(int userId, int taskId);

    /// <summary>
    /// Gets task statistics for a user
    /// </summary>
    Task<TaskStatsResponse> GetTaskStatsAsync(int userId);

    /// <summary>
    /// Validates that a category belongs to the user
    /// </summary>
    Task<bool> ValidateCategoryOwnershipAsync(int userId, int categoryId);
}
