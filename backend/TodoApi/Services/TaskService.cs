using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Services;

/// <summary>
/// Service implementation for task operations
/// </summary>
public class TaskService : ITaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<TaskResponse>> GetTasksAsync(int userId, TaskFilterParams filters)
    {
        var query = BuildFilteredQuery(userId, filters);

        // Apply pagination
        var tasks = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync();

        return tasks.Select(MapToTaskResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<PagedTaskResponse> GetTasksPagedAsync(int userId, TaskFilterParams filters)
    {
        var query = BuildFilteredQuery(userId, filters);

        var totalCount = await query.CountAsync();

        var tasks = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync();

        return new PagedTaskResponse
        {
            Items = tasks.Select(MapToTaskResponse).ToList(),
            TotalCount = totalCount,
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize
        };
    }

    /// <summary>
    /// Builds a filtered query for tasks based on the provided filters
    /// </summary>
    private IQueryable<TodoTask> BuildFilteredQuery(int userId, TaskFilterParams filters)
    {
        var query = _context.Tasks
            .Where(t => t.UserId == userId)
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

        return query;
    }

    /// <inheritdoc />
    public async Task<TaskResponse?> GetTaskAsync(int userId, int taskId)
    {
        var task = await _context.Tasks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        return task == null ? null : MapToTaskResponse(task);
    }

    /// <inheritdoc />
    public async Task<TaskResponse> CreateTaskAsync(int userId, CreateTaskRequest request)
    {
        var task = new TodoTask
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CategoryId = request.CategoryId,
            UserId = userId,
            Status = Models.TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Reload with category
        await _context.Entry(task).Reference(t => t.Category).LoadAsync();

        return MapToTaskResponse(task);
    }

    /// <inheritdoc />
    public async Task<TaskResponse?> UpdateTaskAsync(int userId, int taskId, UpdateTaskRequest request)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            return null;
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

        return MapToTaskResponse(task);
    }

    /// <inheritdoc />
    public async Task<TaskResponse?> UpdateTaskStatusAsync(int userId, int taskId, Models.TaskStatus status)
    {
        var task = await _context.Tasks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            return null;
        }

        task.Status = status;
        await _context.SaveChangesAsync();

        return MapToTaskResponse(task);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTaskAsync(int userId, int taskId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<TaskStatsResponse> GetTaskStatsAsync(int userId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return new TaskStatsResponse
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
    }

    /// <inheritdoc />
    public async Task<bool> ValidateCategoryOwnershipAsync(int userId, int categoryId)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId && c.UserId == userId);
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
