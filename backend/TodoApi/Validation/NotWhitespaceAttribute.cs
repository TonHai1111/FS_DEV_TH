using System.ComponentModel.DataAnnotations;

namespace TodoApi.Validation;

/// <summary>
/// Validates that a string contains at least one non-whitespace character
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotWhitespaceAttribute : ValidationAttribute
{
    public NotWhitespaceAttribute() : base("The {0} field must contain at least one non-whitespace character.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // Use [Required] for null checks
        }

        if (value is string stringValue)
        {
            return !string.IsNullOrWhiteSpace(stringValue);
        }

        return true;
    }
}
