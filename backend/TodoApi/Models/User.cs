using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

/// <summary>
/// Represents a user in the system. Each user owns their own tasks and categories.
/// </summary>
public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Refresh token for JWT token refresh mechanism
    /// </summary>
    public string? RefreshToken { get; set; }
    
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Navigation properties
    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
