using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Models;

namespace TodoApi.Services;

/// <summary>
/// JWT token service implementation for generating and validating tokens
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var jwtSettings = _configuration.GetSection("JwtSettings");
        
        _secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = jwtSettings["Issuer"] ?? "TodoApi";
        _audience = jwtSettings["Audience"] ?? "TodoApp";
        _accessTokenExpirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");
        _refreshTokenExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationInDays"] ?? "7");
    }
    
    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: GetAccessTokenExpiration(),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    /// <inheritdoc />
    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
    }
    
    /// <inheritdoc />
    public DateTime GetRefreshTokenExpiration()
    {
        return DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
    }
}
