using TodoApi.Models.DTOs;

namespace TodoApi.Services;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// Authenticates a user and returns tokens
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Refreshes the access token using a valid refresh token
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Revokes a user's refresh token (logout)
    /// </summary>
    Task RevokeRefreshTokenAsync(int userId);
}
