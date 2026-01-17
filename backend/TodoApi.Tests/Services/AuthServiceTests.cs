using Microsoft.EntityFrameworkCore;
using Moq;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests.Services;

public class AuthServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private Mock<ITokenService> CreateMockTokenService()
    {
        var mock = new Mock<ITokenService>();
        mock.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("test-access-token");
        mock.Setup(x => x.GenerateRefreshToken()).Returns("test-refresh-token");
        mock.Setup(x => x.GetAccessTokenExpiration()).Returns(DateTime.UtcNow.AddHours(1));
        mock.Setup(x => x.GetRefreshTokenExpiration()).Returns(DateTime.UtcNow.AddDays(7));
        return mock;
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserAndReturnsTokens()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();
        var authService = new AuthService(context, tokenService.Object);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("test-refresh-token", result.RefreshToken);
        Assert.Equal("testuser", result.User.Username);
        Assert.Equal("test@example.com", result.User.Email);

        // Verify user was saved to database
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("testuser", savedUser.Username);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        // Add existing user
        context.Users.Add(new User
        {
            Username = "existinguser",
            Email = "test@example.com",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => authService.RegisterAsync(request));

        Assert.Equal("A user with this email already exists", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        // Add existing user
        context.Users.Add(new User
        {
            Username = "testuser",
            Email = "existing@example.com",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "new@example.com",
            Password = "password123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => authService.RegisterAsync(request));

        Assert.Equal("This username is already taken", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_CreatesDefaultCategories()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();
        var authService = new AuthService(context, tokenService.Object);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        var categories = await context.Categories.Where(c => c.UserId == result.User.Id).ToListAsync();
        Assert.Equal(3, categories.Count);
        Assert.Contains(categories, c => c.Name == "Work");
        Assert.Contains(categories, c => c.Name == "Personal");
        Assert.Contains(categories, c => c.Name == "Shopping");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = passwordHash
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("test-refresh-token", result.RefreshToken);
        Assert.Equal("testuser", result.User.Username);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();
        var authService = new AuthService(context, tokenService.Object);

        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.LoginAsync(request));

        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = passwordHash
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.LoginAsync(request));

        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        // Act
        var result = await authService.RefreshTokenAsync("valid-refresh-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("test-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        context.Users.Add(new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            RefreshToken = "expired-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.RefreshTokenAsync("expired-refresh-token"));

        Assert.Equal("Invalid or expired refresh token", exception.Message);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ClearsUserToken()
    {
        // Arrange
        using var context = CreateContext();
        var tokenService = CreateMockTokenService();

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            RefreshToken = "some-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, tokenService.Object);

        // Act
        await authService.RevokeRefreshTokenAsync(user.Id);

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.Null(updatedUser?.RefreshToken);
        Assert.Null(updatedUser?.RefreshTokenExpiryTime);
    }
}
