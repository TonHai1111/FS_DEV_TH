using System.ComponentModel.DataAnnotations;

namespace TodoApi.Validation;

/// <summary>
/// Validates that an enum value is defined in the enum type
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidEnumAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public ValidEnumAttribute(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("Type must be an enum", nameof(enumType));
        }
        _enumType = enumType;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Use [Required] for null checks
        }

        if (!Enum.IsDefined(_enumType, value))
        {
            return new ValidationResult(
                $"The value '{value}' is not a valid {_enumType.Name}. Valid values are: {string.Join(", ", Enum.GetNames(_enumType))}");
        }

        return ValidationResult.Success;
    }
}
