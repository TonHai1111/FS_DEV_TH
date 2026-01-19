using System.ComponentModel.DataAnnotations;
using TodoApi.Validation;

namespace TodoApi.Models.DTOs;

/// <summary>
/// Request DTO for creating a new task
/// </summary>
public class CreateTaskRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    [NotWhitespace]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [ValidEnum(typeof(TaskPriority))]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer")]
    public int? CategoryId { get; set; }
}

/// <summary>
/// Request DTO for updating an existing task
/// </summary>
public class UpdateTaskRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    [NotWhitespace]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [ValidEnum(typeof(Models.TaskStatus))]
    public Models.TaskStatus Status { get; set; }

    [ValidEnum(typeof(TaskPriority))]
    public TaskPriority Priority { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer")]
    public int? CategoryId { get; set; }
}

/// <summary>
/// Request DTO for updating only the task status (for drag-and-drop)
/// </summary>
public class UpdateTaskStatusRequest
{
    [Required]
    [ValidEnum(typeof(Models.TaskStatus))]
    public Models.TaskStatus Status { get; set; }
}

/// <summary>
/// Response DTO for task information
/// </summary>
public class TaskResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Models.TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CategoryId { get; set; }
    public CategoryResponse? Category { get; set; }
    
    /// <summary>
    /// Indicates if the task is overdue
    /// </summary>
    public bool IsOverdue => DueDate.HasValue && 
                             DueDate.Value < DateTime.UtcNow && 
                             Status != Models.TaskStatus.Done;
}

/// <summary>
/// Query parameters for filtering tasks
/// </summary>
public class TaskFilterParams
{
    private const int MaxSearchLength = 200;
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 50;

    public Models.TaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? CategoryId { get; set; }

    private string? _search;
    /// <summary>
    /// Search term limited to MaxSearchLength characters
    /// </summary>
    public string? Search
    {
        get => _search;
        set => _search = value?.Length > MaxSearchLength ? value[..MaxSearchLength] : value;
    }

    public bool? Overdue { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public bool SortDescending { get; set; } = true;

    private int _pageNumber = 1;
    /// <summary>
    /// Page number (1-based), defaults to 1
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = Math.Max(1, value);
    }

    private int _pageSize = DefaultPageSize;
    /// <summary>
    /// Number of items per page, defaults to 50, max 100
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
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

/// <summary>
/// Response DTO for paginated task results
/// </summary>
public class PagedTaskResponse
{
    public List<TaskResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
