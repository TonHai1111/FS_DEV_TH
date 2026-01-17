using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests.Controllers;

public class AuthControllerTests
{
    private Mock<IAuthService> CreateMockAuthService()
    {
        return new Mock<IAuthService>();
    }

    private AuthController CreateControllerWithUser(Mock<IAuthService> mockAuthService, int userId = 1)
    {
        var controller = new AuthController(mockAuthService.Object);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }

    [Fact]
    public async Task Register_WithValidRequest_Returns201Created()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        var authResponse = new AuthResponse
        {
            AccessToken = "test-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserResponse { Id = 1, Username = "testuser", Email = "test@example.com" }
        };

        mockAuthService
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(authResponse);

        var controller = new AuthController(mockAuthService.Object);
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await controller.Register(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<AuthResponse>>(objectResult.Value);
        Assert.True(response.Success);
        Assert.Equal("test-token", response.Data!.AccessToken);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        mockAuthService
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new InvalidOperationException("A user with this email already exists"));

        var controller = new AuthController(mockAuthService.Object);
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "password123"
        };

        // Act
        var result = await controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("A user with this email already exists", response.Message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        var authResponse = new AuthResponse
        {
            AccessToken = "test-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserResponse { Id = 1, Username = "testuser", Email = "test@example.com" }
        };

        mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(authResponse);

        var controller = new AuthController(mockAuthService.Object);
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Login successful", response.Message);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        var controller = new AuthController(mockAuthService.Object);
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid email or password", response.Message);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_Returns200Ok()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        var authResponse = new AuthResponse
        {
            AccessToken = "new-token",
            RefreshToken = "new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserResponse { Id = 1, Username = "testuser", Email = "test@example.com" }
        };

        mockAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(authResponse);

        var controller = new AuthController(mockAuthService.Object);
        var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };

        // Act
        var result = await controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("new-token", response.Data!.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_Returns401Unauthorized()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        mockAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid or expired refresh token"));

        var controller = new AuthController(mockAuthService.Object);
        var request = new RefreshTokenRequest { RefreshToken = "expired-token" };

        // Act
        var result = await controller.RefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Logout_Returns200Ok()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        mockAuthService
            .Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var controller = CreateControllerWithUser(mockAuthService, userId: 1);

        // Act
        var result = await controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Logged out successfully", response.Message);

        mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(1), Times.Once);
    }

    [Fact]
    public void GetCurrentUser_ReturnsUserInfo()
    {
        // Arrange
        var mockAuthService = CreateMockAuthService();
        var controller = CreateControllerWithUser(mockAuthService, userId: 1);

        // Act
        var result = controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Data!.Id);
        Assert.Equal("testuser", response.Data.Username);
        Assert.Equal("test@example.com", response.Data.Email);
    }
}