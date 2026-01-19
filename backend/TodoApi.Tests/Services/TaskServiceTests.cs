using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests.Services;

public class TaskServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task<User> CreateTestUser(AppDbContext context)
    {
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsUserTasks()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Tasks.AddRange(
            new TodoTask { Title = "Task 1", UserId = user.Id },
            new TodoTask { Title = "Task 2", UserId = user.Id },
            new TodoTask { Title = "Other User Task", UserId = 999 }
        );
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.GetTasksAsync(user.Id, new TaskFilterParams());

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Contains("Task", t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_FiltersbyStatus()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Tasks.AddRange(
            new TodoTask { Title = "Todo Task", UserId = user.Id, Status = Models.TaskStatus.Todo },
            new TodoTask { Title = "Done Task", UserId = user.Id, Status = Models.TaskStatus.Done }
        );
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);
        var filters = new TaskFilterParams { Status = Models.TaskStatus.Todo };

        // Act
        var result = await taskService.GetTasksAsync(user.Id, filters);

        // Assert
        Assert.Single(result);
        Assert.Equal("Todo Task", result[0].Title);
    }

    [Fact]
    public async Task GetTasksAsync_FiltersByPriority()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Tasks.AddRange(
            new TodoTask { Title = "High Priority", UserId = user.Id, Priority = TaskPriority.High },
            new TodoTask { Title = "Low Priority", UserId = user.Id, Priority = TaskPriority.Low }
        );
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);
        var filters = new TaskFilterParams { Priority = TaskPriority.High };

        // Act
        var result = await taskService.GetTasksAsync(user.Id, filters);

        // Assert
        Assert.Single(result);
        Assert.Equal("High Priority", result[0].Title);
    }

    [Fact]
    public async Task GetTasksAsync_SearchesByTitle()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Tasks.AddRange(
            new TodoTask { Title = "Buy groceries", UserId = user.Id },
            new TodoTask { Title = "Call doctor", UserId = user.Id }
        );
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);
        var filters = new TaskFilterParams { Search = "groceries" };

        // Act
        var result = await taskService.GetTasksAsync(user.Id, filters);

        // Assert
        Assert.Single(result);
        Assert.Equal("Buy groceries", result[0].Title);
    }

    [Fact]
    public async Task GetTaskAsync_ReturnsTask_WhenExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var task = new TodoTask { Title = "Test Task", UserId = user.Id };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.GetTaskAsync(user.Id, task.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Task", result.Title);
    }

    [Fact]
    public async Task GetTaskAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var taskService = new TaskService(context);

        // Act
        var result = await taskService.GetTaskAsync(user.Id, 999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaskAsync_ReturnsNull_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var task = new TodoTask { Title = "Other User Task", UserId = 999 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.GetTaskAsync(user.Id, task.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTaskAsync_CreatesAndReturnsTask()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var taskService = new TaskService(context);

        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Task description",
            Priority = TaskPriority.High
        };

        // Act
        var result = await taskService.CreateTaskAsync(user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Task", result.Title);
        Assert.Equal("Task description", result.Description);
        Assert.Equal(TaskPriority.High, result.Priority);
        Assert.Equal(Models.TaskStatus.Todo, result.Status);

        // Verify saved in database
        var savedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Title == "New Task");
        Assert.NotNull(savedTask);
    }

    [Fact]
    public async Task UpdateTaskAsync_UpdatesAndReturnsTask()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var task = new TodoTask { Title = "Old Title", UserId = user.Id };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        var request = new UpdateTaskRequest
        {
            Title = "New Title",
            Status = Models.TaskStatus.InProgress,
            Priority = TaskPriority.High
        };

        // Act
        var result = await taskService.UpdateTaskAsync(user.Id, task.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal(Models.TaskStatus.InProgress, result.Status);
        Assert.Equal(TaskPriority.High, result.Priority);
    }

    [Fact]
    public async Task UpdateTaskAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var taskService = new TaskService(context);

        var request = new UpdateTaskRequest { Title = "New Title" };

        // Act
        var result = await taskService.UpdateTaskAsync(user.Id, 999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_UpdatesStatus()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var task = new TodoTask { Title = "Task", UserId = user.Id, Status = Models.TaskStatus.Todo };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.UpdateTaskStatusAsync(user.Id, task.Id, Models.TaskStatus.Done);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Models.TaskStatus.Done, result.Status);
    }

    [Fact]
    public async Task DeleteTaskAsync_ReturnsTrue_WhenDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var task = new TodoTask { Title = "Task to delete", UserId = user.Id };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.DeleteTaskAsync(user.Id, task.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.Tasks.FindAsync(task.Id));
    }

    [Fact]
    public async Task DeleteTaskAsync_ReturnsFalse_WhenNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var taskService = new TaskService(context);

        // Act
        var result = await taskService.DeleteTaskAsync(user.Id, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetTaskStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Tasks.AddRange(
            new TodoTask { Title = "Task 1", UserId = user.Id, Status = Models.TaskStatus.Todo },
            new TodoTask { Title = "Task 2", UserId = user.Id, Status = Models.TaskStatus.Todo },
            new TodoTask { Title = "Task 3", UserId = user.Id, Status = Models.TaskStatus.InProgress },
            new TodoTask { Title = "Task 4", UserId = user.Id, Status = Models.TaskStatus.Done },
            new TodoTask { Title = "Task 5", UserId = user.Id, Status = Models.TaskStatus.Todo, DueDate = DateTime.UtcNow.AddDays(-1) }
        );
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.GetTaskStatsAsync(user.Id);

        // Assert
        Assert.Equal(5, result.Total);
        Assert.Equal(3, result.Todo);
        Assert.Equal(1, result.InProgress);
        Assert.Equal(1, result.Done);
        Assert.Equal(1, result.Overdue);
    }

    [Fact]
    public async Task ValidateCategoryOwnershipAsync_ReturnsTrue_WhenOwned()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Work", UserId = user.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.ValidateCategoryOwnershipAsync(user.Id, category.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCategoryOwnershipAsync_ReturnsFalse_WhenNotOwned()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Other User Category", UserId = 999 };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var result = await taskService.ValidateCategoryOwnershipAsync(user.Id, category.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetTasksAsync_AppliesPagination()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        // Create 10 tasks
        for (int i = 1; i <= 10; i++)
        {
            context.Tasks.Add(new TodoTask
            {
                Title = $"Task {i}",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act - Get first page of 3 items
        var filters = new TaskFilterParams { PageNumber = 1, PageSize = 3, SortDescending = false };
        var result = await taskService.GetTasksAsync(user.Id, filters);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsCorrectPage()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        // Create 10 tasks
        for (int i = 1; i <= 10; i++)
        {
            context.Tasks.Add(new TodoTask
            {
                Title = $"Task {i:D2}",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act - Get second page (page 2) of 3 items, sorted ascending by CreatedAt
        var filters = new TaskFilterParams { PageNumber = 2, PageSize = 3, SortDescending = false };
        var result = await taskService.GetTasksAsync(user.Id, filters);

        // Assert - Should return tasks 4, 5, 6
        Assert.Equal(3, result.Count);
        Assert.Equal("Task 04", result[0].Title);
        Assert.Equal("Task 05", result[1].Title);
        Assert.Equal("Task 06", result[2].Title);
    }

    [Fact]
    public async Task GetTasksPagedAsync_ReturnsCorrectPaginationMetadata()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        // Create 10 tasks
        for (int i = 1; i <= 10; i++)
        {
            context.Tasks.Add(new TodoTask { Title = $"Task {i}", UserId = user.Id });
        }
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act
        var filters = new TaskFilterParams { PageNumber = 2, PageSize = 3 };
        var result = await taskService.GetTasksPagedAsync(user.Id, filters);

        // Assert
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(4, result.TotalPages); // 10 items / 3 per page = 4 pages (rounded up)
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetTasksPagedAsync_LastPage_HasNoNextPage()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        // Create 5 tasks
        for (int i = 1; i <= 5; i++)
        {
            context.Tasks.Add(new TodoTask { Title = $"Task {i}", UserId = user.Id });
        }
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act - Get last page
        var filters = new TaskFilterParams { PageNumber = 2, PageSize = 3 };
        var result = await taskService.GetTasksPagedAsync(user.Id, filters);

        // Assert
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
        Assert.Equal(2, result.Items.Count); // Last page has only 2 items
    }

    [Fact]
    public async Task GetTasksPagedAsync_FirstPage_HasNoPreviousPage()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        // Create 5 tasks
        for (int i = 1; i <= 5; i++)
        {
            context.Tasks.Add(new TodoTask { Title = $"Task {i}", UserId = user.Id });
        }
        await context.SaveChangesAsync();

        var taskService = new TaskService(context);

        // Act - Get first page
        var filters = new TaskFilterParams { PageNumber = 1, PageSize = 3 };
        var result = await taskService.GetTasksPagedAsync(user.Id, filters);

        // Assert
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void TaskFilterParams_SearchTruncatesToMaxLength()
    {
        // Arrange
        var longSearch = new string('a', 300); // 300 characters

        // Act
        var filters = new TaskFilterParams { Search = longSearch };

        // Assert - Search should be truncated to 200 characters
        Assert.Equal(200, filters.Search?.Length);
    }

    [Fact]
    public void TaskFilterParams_PageSize_ValidationFailsWhenExceedsMaximum()
    {
        // Arrange
        var filters = new TaskFilterParams { PageSize = 500 };
        var validationContext = new ValidationContext(filters);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(filters, validationContext, validationResults, true);

        // Assert - PageSize exceeding 100 should fail validation
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("PageSize"));
    }

    [Fact]
    public void TaskFilterParams_PageNumber_ValidationFailsWhenLessThanOne()
    {
        // Arrange
        var filters = new TaskFilterParams { PageNumber = -5 };
        var validationContext = new ValidationContext(filters);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(filters, validationContext, validationResults, true);

        // Assert - PageNumber less than 1 should fail validation
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("PageNumber"));
    }

    [Fact]
    public void TaskFilterParams_PageSize_ValidationFailsWhenZeroOrNegative()
    {
        // Arrange
        var filters = new TaskFilterParams { PageSize = 0 };
        var validationContext = new ValidationContext(filters);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(filters, validationContext, validationResults, true);

        // Assert - PageSize of 0 should fail validation
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("PageSize"));
    }

    [Fact]
    public void TaskFilterParams_DefaultValues_AreValid()
    {
        // Arrange
        var filters = new TaskFilterParams();
        var validationContext = new ValidationContext(filters);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(filters, validationContext, validationResults, true);

        // Assert - Default values should be valid
        Assert.True(isValid);
        Assert.Equal(1, filters.PageNumber);
        Assert.Equal(50, filters.PageSize);
    }
}
