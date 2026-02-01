using FluentValidation;
using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Validators;

/// <summary>
/// Validator for creating a new task
/// </summary>
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Invalid category ID")
            .When(x => x.CategoryId.HasValue);
    }
}

/// <summary>
/// Validator for updating an existing task
/// </summary>
public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Invalid category ID")
            .When(x => x.CategoryId.HasValue);
    }
}

/// <summary>
/// Validator for updating task status (drag-and-drop)
/// </summary>
public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value");
    }
}

/// <summary>
/// Validator for task filter parameters
/// </summary>
public class TaskFilterParamsValidator : AbstractValidator<TaskFilterParams>
{
    public TaskFilterParamsValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Invalid category ID")
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Search));

        // Note: SortBy validation removed - DTO normalizes invalid values to "createdAt" default,
        // and TaskService handles unknown values with a fallback sort
    }
}