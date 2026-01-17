using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests.Services;

public class CategoryServiceTests
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
    public async Task GetCategoriesAsync_ReturnsUserCategories()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Categories.AddRange(
            new Category { Name = "Work", Color = "#FF0000", UserId = user.Id },
            new Category { Name = "Personal", Color = "#00FF00", UserId = user.Id },
            new Category { Name = "Other User Category", Color = "#0000FF", UserId = 999 }
        );
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.GetCategoriesAsync(user.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "Work");
        Assert.Contains(result, c => c.Name == "Personal");
        Assert.DoesNotContain(result, c => c.Name == "Other User Category");
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesOrderedByName()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Categories.AddRange(
            new Category { Name = "Zebra", Color = "#FF0000", UserId = user.Id },
            new Category { Name = "Alpha", Color = "#00FF00", UserId = user.Id },
            new Category { Name = "Middle", Color = "#0000FF", UserId = user.Id }
        );
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.GetCategoriesAsync(user.Id);

        // Assert
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Middle", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    [Fact]
    public async Task GetCategoriesAsync_IncludesTaskCount()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Work", Color = "#FF0000", UserId = user.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Tasks.AddRange(
            new TodoTask { Title = "Task 1", UserId = user.Id, CategoryId = category.Id },
            new TodoTask { Title = "Task 2", UserId = user.Id, CategoryId = category.Id }
        );
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.GetCategoriesAsync(user.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].TaskCount);
    }

    [Fact]
    public async Task GetCategoryAsync_ReturnsCategory_WhenExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Work", Color = "#FF0000", UserId = user.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.GetCategoryAsync(user.Id, category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Work", result.Name);
        Assert.Equal("#FF0000", result.Color);
    }

    [Fact]
    public async Task GetCategoryAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.GetCategoryAsync(user.Id, 999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCategoryAsync_ReturnsNull_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Other Category", Color = "#FF0000", UserId = 999 };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.GetCategoryAsync(user.Id, category.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCategoryAsync_CreatesAndReturnsCategory()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var categoryService = new CategoryService(context);

        var request = new CreateCategoryRequest
        {
            Name = "New Category",
            Color = "#FF0000"
        };

        // Act
        var result = await categoryService.CreateCategoryAsync(user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Category", result.Name);
        Assert.Equal("#FF0000", result.Color);
        Assert.Equal(0, result.TaskCount);

        // Verify saved in database
        var savedCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "New Category");
        Assert.NotNull(savedCategory);
        Assert.Equal(user.Id, savedCategory.UserId);
    }

    [Fact]
    public async Task UpdateCategoryAsync_UpdatesAndReturnsCategory()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Old Name", Color = "#000000", UserId = user.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        var request = new UpdateCategoryRequest
        {
            Name = "New Name",
            Color = "#FF0000"
        };

        // Act
        var result = await categoryService.UpdateCategoryAsync(user.Id, category.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("#FF0000", result.Color);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var categoryService = new CategoryService(context);

        var request = new UpdateCategoryRequest
        {
            Name = "New Name",
            Color = "#FF0000"
        };

        // Act
        var result = await categoryService.UpdateCategoryAsync(user.Id, 999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsNull_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Other Category", Color = "#000000", UserId = 999 };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        var request = new UpdateCategoryRequest
        {
            Name = "New Name",
            Color = "#FF0000"
        };

        // Act
        var result = await categoryService.UpdateCategoryAsync(user.Id, category.Id, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsTrue_WhenDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "To Delete", Color = "#FF0000", UserId = user.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.DeleteCategoryAsync(user.Id, category.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.Categories.FindAsync(category.Id));
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.DeleteCategoryAsync(user.Id, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Other Category", Color = "#FF0000", UserId = 999 };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.DeleteCategoryAsync(user.Id, category.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CategoryNameExistsAsync_ReturnsTrue_WhenNameExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Categories.Add(new Category { Name = "Work", Color = "#FF0000", UserId = user.Id });
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.CategoryNameExistsAsync(user.Id, "Work");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CategoryNameExistsAsync_ReturnsFalse_WhenNameNotExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);
        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.CategoryNameExistsAsync(user.Id, "NonExistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CategoryNameExistsAsync_IsCaseInsensitive()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Categories.Add(new Category { Name = "Work", Color = "#FF0000", UserId = user.Id });
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.CategoryNameExistsAsync(user.Id, "WORK");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CategoryNameExistsAsync_ExcludesSpecifiedCategory()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        var category = new Category { Name = "Work", Color = "#FF0000", UserId = user.Id };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act - check if "Work" exists, excluding the category itself
        var result = await categoryService.CategoryNameExistsAsync(user.Id, "Work", category.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CategoryNameExistsAsync_ReturnsFalse_ForOtherUserCategories()
    {
        // Arrange
        using var context = CreateContext();
        var user = await CreateTestUser(context);

        context.Categories.Add(new Category { Name = "Work", Color = "#FF0000", UserId = 999 });
        await context.SaveChangesAsync();

        var categoryService = new CategoryService(context);

        // Act
        var result = await categoryService.CategoryNameExistsAsync(user.Id, "Work");

        // Assert
        Assert.False(result);
    }
}