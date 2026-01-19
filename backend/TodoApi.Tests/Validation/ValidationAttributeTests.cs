using System.ComponentModel.DataAnnotations;
using TodoApi.Models;
using TodoApi.Validation;
using TaskStatus = TodoApi.Models.TaskStatus;

namespace TodoApi.Tests.Validation;

public class ValidationAttributeTests
{
    #region NotWhitespaceAttribute Tests

    [Fact]
    public void NotWhitespaceAttribute_WithNonWhitespaceString_ReturnsTrue()
    {
        // Arrange
        var attribute = new NotWhitespaceAttribute();

        // Act
        var result = attribute.IsValid("Hello World");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NotWhitespaceAttribute_WithWhitespaceOnlyString_ReturnsFalse()
    {
        // Arrange
        var attribute = new NotWhitespaceAttribute();

        // Act
        var result = attribute.IsValid("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NotWhitespaceAttribute_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var attribute = new NotWhitespaceAttribute();

        // Act
        var result = attribute.IsValid("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NotWhitespaceAttribute_WithNull_ReturnsTrue()
    {
        // Arrange
        var attribute = new NotWhitespaceAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert - Null is valid (use [Required] for null checks)
        Assert.True(result);
    }

    [Fact]
    public void NotWhitespaceAttribute_WithMixedContent_ReturnsTrue()
    {
        // Arrange
        var attribute = new NotWhitespaceAttribute();

        // Act
        var result = attribute.IsValid("  a  ");

        // Assert - Contains at least one non-whitespace character
        Assert.True(result);
    }

    [Fact]
    public void NotWhitespaceAttribute_WithTabsAndNewlines_ReturnsFalse()
    {
        // Arrange
        var attribute = new NotWhitespaceAttribute();

        // Act
        var result = attribute.IsValid("\t\n\r ");

        // Assert - Only whitespace characters
        Assert.False(result);
    }

    #endregion

    #region ValidEnumAttribute Tests

    [Fact]
    public void ValidEnumAttribute_WithValidEnumValue_ReturnsSuccess()
    {
        // Arrange
        var attribute = new ValidEnumAttribute(typeof(TaskStatus));
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(TaskStatus.Todo, context);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void ValidEnumAttribute_WithInvalidEnumValue_ReturnsError()
    {
        // Arrange
        var attribute = new ValidEnumAttribute(typeof(TaskStatus));
        var context = new ValidationContext(new object());

        // Act - Cast an invalid value to the enum
        var result = attribute.GetValidationResult((TaskStatus)99, context);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("not a valid TaskStatus", result?.ErrorMessage);
    }

    [Fact]
    public void ValidEnumAttribute_WithNull_ReturnsSuccess()
    {
        // Arrange
        var attribute = new ValidEnumAttribute(typeof(TaskStatus));
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(null, context);

        // Assert - Null is valid (use [Required] for null checks)
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void ValidEnumAttribute_WithValidPriorityValue_ReturnsSuccess()
    {
        // Arrange
        var attribute = new ValidEnumAttribute(typeof(TaskPriority));
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(TaskPriority.High, context);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void ValidEnumAttribute_Constructor_ThrowsForNonEnumType()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidEnumAttribute(typeof(string)));
    }

    [Fact]
    public void ValidEnumAttribute_ErrorMessage_ContainsValidValues()
    {
        // Arrange
        var attribute = new ValidEnumAttribute(typeof(TaskStatus));
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult((TaskStatus)99, context);

        // Assert - Error message should list valid values
        Assert.Contains("Todo", result?.ErrorMessage);
        Assert.Contains("InProgress", result?.ErrorMessage);
        Assert.Contains("Done", result?.ErrorMessage);
    }

    #endregion
}
