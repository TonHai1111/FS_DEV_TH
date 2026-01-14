using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

/// <summary>
/// Represents a category/tag for organizing tasks. Categories are user-specific.
/// </summary>
public class Category
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Hex color code for the category (e.g., "#FF5733")
    /// </summary>
    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";
    
    // Foreign key
    public int UserId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
}
