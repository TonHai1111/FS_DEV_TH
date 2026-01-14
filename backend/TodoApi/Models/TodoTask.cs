using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

/// <summary>
/// Represents a to-do task with status workflow, priority, and optional due date.
/// </summary>
public class TodoTask
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Task status: Todo (0), InProgress (1), Done (2)
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    
    /// <summary>
    /// Task priority: Low (0), Medium (1), High (2)
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    
    /// <summary>
    /// Optional due date for the task
    /// </summary>
    public DateTime? DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int UserId { get; set; }
    
    /// <summary>
    /// Optional category assignment
    /// </summary>
    public int? CategoryId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Category? Category { get; set; }
}

/// <summary>
/// Task status workflow states
/// </summary>
public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

/// <summary>
/// Task priority levels
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
