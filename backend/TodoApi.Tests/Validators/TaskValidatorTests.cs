using FluentValidation.TestHelper;
using TodoApi.Models;
using TodoApi.Models.DTOs;
using TodoApi.Validators;

namespace TodoApi.Tests.Validators;

public class TaskValidatorTests
{
    private readonly CreateTaskRequestValidator _createValidator = new();
    private readonly UpdateTaskRequestValidator _updateValidator = new();

    [Fact]
    public void CreateTask_WithPastDueDate_ShouldHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            DueDate = DateTime.UtcNow.AddDays(-1) // Past date
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage("Due date must be in the future");
    }

    [Fact]
    public void CreateTask_WithFutureDueDate_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            DueDate = DateTime.UtcNow.AddDays(1) // Future date
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void CreateTask_WithNullDueDate_ShouldNotHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            DueDate = null // No due date
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void CreateTask_WithEmptyTitle_ShouldHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = ""
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required");
    }

    [Fact]
    public void CreateTask_WithTitleExceeding200Chars_ShouldHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = new string('a', 201)
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters");
    }

    [Fact]
    public void CreateTask_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task Title",
            Description = "This is a valid description",
            Priority = TaskPriority.Medium,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateTask_WithInvalidCategoryId_ShouldHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            CategoryId = 0 // Invalid
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Invalid category ID");
    }

    [Fact]
    public void CreateTask_WithDescriptionExceeding2000Chars_ShouldHaveError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            Description = new string('a', 2001)
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 2000 characters");
    }
}