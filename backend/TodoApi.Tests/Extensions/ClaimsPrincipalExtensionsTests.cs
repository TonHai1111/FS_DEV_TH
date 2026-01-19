using System.Security.Claims;
using TodoApi.Extensions;

namespace TodoApi.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserIdOrThrow_WithValidClaim_ReturnsUserId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrThrow();

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void GetUserIdOrThrow_WithMissingClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => principal.GetUserIdOrThrow());
        Assert.Equal("User ID claim is missing", exception.Message);
    }

    [Fact]
    public void GetUserIdOrThrow_WithEmptyClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => principal.GetUserIdOrThrow());
        Assert.Equal("User ID claim is missing", exception.Message);
    }

    [Fact]
    public void GetUserIdOrThrow_WithInvalidClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => principal.GetUserIdOrThrow());
        Assert.Equal("User ID claim is invalid", exception.Message);
    }

    [Fact]
    public void GetUsernameOrThrow_WithValidClaim_ReturnsUsername()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUsernameOrThrow();

        // Assert
        Assert.Equal("testuser", result);
    }

    [Fact]
    public void GetUsernameOrThrow_WithMissingClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => principal.GetUsernameOrThrow());
        Assert.Equal("Username claim is missing", exception.Message);
    }

    [Fact]
    public void GetEmailOrThrow_WithValidClaim_ReturnsEmail()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetEmailOrThrow();

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void GetEmailOrThrow_WithMissingClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => principal.GetEmailOrThrow());
        Assert.Equal("Email claim is missing", exception.Message);
    }
}
