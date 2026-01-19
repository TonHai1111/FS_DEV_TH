using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Models.DTOs;

namespace TodoApi.Services;

/// <summary>
/// Authentication service implementation handling user registration, login, and token management
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    
    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }
    
    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("A user with this email already exists");
        }
        
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("This username is already taken");
        }
        
        // Create new user
        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };
        
        // Generate tokens
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiration();
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Create default categories for the new user
        await CreateDefaultCategoriesAsync(user.Id);
        
        return CreateAuthResponse(user);
    }
    
    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // Update refresh token
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiration();
        
        await _context.SaveChangesAsync();
        
        return CreateAuthResponse(user);
    }
    
    /// <inheritdoc />
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        // Check for invalid/expired token - handle null RefreshTokenExpiryTime
        if (user == null ||
            !user.RefreshTokenExpiryTime.HasValue ||
            user.RefreshTokenExpiryTime.Value <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Generate new tokens
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = _tokenService.GetRefreshTokenExpiration();

        await _context.SaveChangesAsync();

        return CreateAuthResponse(user);
    }
    
    /// <inheritdoc />
    public async Task RevokeRefreshTokenAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Creates default categories for a new user
    /// </summary>
    private async Task CreateDefaultCategoriesAsync(int userId)
    {
        var defaultCategories = new List<Category>
        {
            new() { Name = "Work", Color = "#3B82F6", UserId = userId },
            new() { Name = "Personal", Color = "#10B981", UserId = userId },
            new() { Name = "Shopping", Color = "#F59E0B", UserId = userId }
        };
        
        _context.Categories.AddRange(defaultCategories);
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Creates an AuthResponse from a User entity
    /// </summary>
    private AuthResponse CreateAuthResponse(User user)
    {
        return new AuthResponse
        {
            AccessToken = _tokenService.GenerateAccessToken(user),
            RefreshToken = user.RefreshToken!,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            }
        };
    }
}
