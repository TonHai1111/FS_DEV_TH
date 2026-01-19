using System.Security.Claims;

namespace TodoApi.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to safely extract user claims
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Generic helper to extract a string claim or throw UnauthorizedAccessException
    /// </summary>
    private static string GetClaimOrThrow(this ClaimsPrincipal user, string claimType, string claimName)
    {
        var value = user.FindFirstValue(claimType);
        if (string.IsNullOrEmpty(value))
        {
            throw new UnauthorizedAccessException($"{claimName} claim is missing");
        }
        return value;
    }

    /// <summary>
    /// Safely extracts the user ID from claims, throwing UnauthorizedAccessException if invalid
    /// </summary>
    public static int GetUserIdOrThrow(this ClaimsPrincipal user)
    {
        var userIdClaim = user.GetClaimOrThrow(ClaimTypes.NameIdentifier, "User ID");

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim is invalid");
        }

        return userId;
    }

    /// <summary>
    /// Safely extracts the username from claims, throwing UnauthorizedAccessException if missing
    /// </summary>
    public static string GetUsernameOrThrow(this ClaimsPrincipal user)
        => user.GetClaimOrThrow(ClaimTypes.Name, "Username");

    /// <summary>
    /// Safely extracts the email from claims, throwing UnauthorizedAccessException if missing
    /// </summary>
    public static string GetEmailOrThrow(this ClaimsPrincipal user)
        => user.GetClaimOrThrow(ClaimTypes.Email, "Email");
}
