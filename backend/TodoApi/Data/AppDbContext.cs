using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/// <summary>
/// Entity Framework Core database context for the Todo application.
/// Configures entity relationships and applies model constraints.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<TodoTask> Tasks => Set<TodoTask>();
    public DbSet<Category> Categories => Set<Category>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            
            entity.HasMany(u => u.Tasks)
                  .WithOne(t => t.User)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasMany(u => u.Categories)
                  .WithOne(c => c.User)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            // Ensure category names are unique per user
            entity.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
            
            entity.HasMany(c => c.Tasks)
                  .WithOne(t => t.Category)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        // TodoTask configuration
        modelBuilder.Entity<TodoTask>(entity =>
        {
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.DueDate);
            
            entity.Property(t => t.Status)
                  .HasConversion<int>();
                  
            entity.Property(t => t.Priority)
                  .HasConversion<int>();
        });
    }
    
    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<TodoTask>()
            .Where(e => e.State == EntityState.Modified);
            
        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
