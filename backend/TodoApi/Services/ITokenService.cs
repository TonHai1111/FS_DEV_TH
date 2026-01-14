using TodoApi.Models;

namespace TodoApi.Services;

/// <summary>
/// Service interface for JWT token generation and validation
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the user
    /// </summary>
    string GenerateAccessToken(User user);
    
    /// <summary>
    /// Generates a refresh token
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Gets the expiration time for access tokens
    /// </summary>
    DateTime GetAccessTokenExpiration();
    
    /// <summary>
    /// Gets the expiration time for refresh tokens
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}
