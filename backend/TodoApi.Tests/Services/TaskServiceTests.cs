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
}
