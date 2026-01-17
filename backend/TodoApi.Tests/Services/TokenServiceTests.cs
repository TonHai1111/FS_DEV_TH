using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests.Services;

public class TokenServiceTests
{
    private IConfiguration CreateConfiguration(
        string secretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
        string issuer = "TodoApi",
        string audience = "TodoApp",
        string expirationMinutes = "60",
        string refreshTokenDays = "7")
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", secretKey },
            { "JwtSettings:Issuer", issuer },
            { "JwtSettings:Audience", audience },
            { "JwtSettings:ExpirationInMinutes", expirationMinutes },
            { "JwtSettings:RefreshTokenExpirationInDays", refreshTokenDays }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private User CreateTestUser()
    {
        return new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash"
        };
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtToken()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var tokenService = new TokenService(configuration);
        var user = CreateTestUser();

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify it's a valid JWT (has 3 parts separated by dots)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var tokenService = new TokenService(configuration);
        var user = CreateTestUser();

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.Username, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.NotNull(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var configuration = CreateConfiguration(issuer: "TestIssuer", audience: "TestAudience");
        var tokenService = new TokenService(configuration);
        var user = CreateTestUser();

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var tokenService = new TokenService(configuration);

        // Act
        var refreshToken = tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var tokenService = new TokenService(configuration);

        // Act
        var refreshToken = tokenService.GenerateRefreshToken();

        // Assert - should be valid base64
        var buffer = new Span<byte>(new byte[refreshToken.Length]);
        Assert.True(Convert.TryFromBase64String(refreshToken, buffer, out _));
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var tokenService = new TokenService(configuration);

        // Act
        var token1 = tokenService.GenerateRefreshToken();
        var token2 = tokenService.GenerateRefreshToken();
        var token3 = tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token2, token3);
        Assert.NotEqual(token1, token3);
    }

    [Fact]
    public void GetAccessTokenExpiration_ReturnsFutureDate()
    {
        // Arrange
        var configuration = CreateConfiguration(expirationMinutes: "60");
        var tokenService = new TokenService(configuration);

        // Act
        var expiration = tokenService.GetAccessTokenExpiration();

        // Assert
        Assert.True(expiration > DateTime.UtcNow);
        Assert.True(expiration <= DateTime.UtcNow.AddMinutes(61)); // Allow 1 minute tolerance
    }

    [Fact]
    public void GetAccessTokenExpiration_RespectsConfiguredExpiration()
    {
        // Arrange
        var configuration = CreateConfiguration(expirationMinutes: "30");
        var tokenService = new TokenService(configuration);
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiration = tokenService.GetAccessTokenExpiration();

        // Assert
        var expectedMin = beforeCall.AddMinutes(29);
        var expectedMax = beforeCall.AddMinutes(31);
        Assert.True(expiration >= expectedMin && expiration <= expectedMax);
    }

    [Fact]
    public void GetRefreshTokenExpiration_ReturnsFutureDate()
    {
        // Arrange
        var configuration = CreateConfiguration(refreshTokenDays: "7");
        var tokenService = new TokenService(configuration);

        // Act
        var expiration = tokenService.GetRefreshTokenExpiration();

        // Assert
        Assert.True(expiration > DateTime.UtcNow);
        Assert.True(expiration <= DateTime.UtcNow.AddDays(8)); // Allow 1 day tolerance
    }

    [Fact]
    public void GetRefreshTokenExpiration_RespectsConfiguredExpiration()
    {
        // Arrange
        var configuration = CreateConfiguration(refreshTokenDays: "14");
        var tokenService = new TokenService(configuration);
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiration = tokenService.GetRefreshTokenExpiration();

        // Assert
        var expectedMin = beforeCall.AddDays(13);
        var expectedMax = beforeCall.AddDays(15);
        Assert.True(expiration >= expectedMin && expiration <= expectedMax);
    }

    [Fact]
    public void Constructor_ThrowsException_WhenSecretKeyMissing()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:Issuer", "TodoApi" }
            // SecretKey is missing
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new TokenService(configuration));
    }

    [Fact]
    public void Constructor_UsesDefaultValues_WhenOptionalSettingsMissing()
    {
        // Arrange - only provide SecretKey
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act - should not throw
        var tokenService = new TokenService(configuration);
        var user = new User { Id = 1, Username = "test", Email = "test@example.com", PasswordHash = "hash" };

        // Assert - verify defaults work
        var token = tokenService.GenerateAccessToken(user);
        Assert.NotNull(token);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal("TodoApi", jwtToken.Issuer); // Default issuer
        Assert.Contains("TodoApp", jwtToken.Audiences); // Default audience
    }
}