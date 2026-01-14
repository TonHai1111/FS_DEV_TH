using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models.DTOs;

/// <summary>
/// Request DTO for creating a new task
/// </summary>
public class CreateTaskRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    
    public DateTime? DueDate { get; set; }
    
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
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public Models.TaskStatus Status { get; set; }
    
    public TaskPriority Priority { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public int? CategoryId { get; set; }
}

/// <summary>
/// Request DTO for updating only the task status (for drag-and-drop)
/// </summary>
public class UpdateTaskStatusRequest
{
    [Required]
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
    public Models.TaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public bool? Overdue { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public bool SortDescending { get; set; } = true;
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
