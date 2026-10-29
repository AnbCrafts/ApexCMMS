using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;
using ApexCMMS.Infrastructure.Data;

namespace ApexCMMS.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public JwtTokenService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(
        ApplicationUser user,
        IList<string> roles)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? "ApexCMMS_SuperSecretKey_ProductionGrade_2026_Minimum32CharsLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "ApexCMMS";
        var audience = _configuration["JwtSettings:Audience"] ?? "ApexCMMS-FieldApp";
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("EmployeeCode", user.EmployeeCode),
            new("FullName", user.FullName),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (user.DepartmentId.HasValue)
        {
            claims.Add(new Claim("DepartmentId", user.DepartmentId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(securityToken);

        // Generate cryptographically secure refresh token
        var refreshTokenString = GenerateRefreshTokenString();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            JwtId = securityToken.Id,
            IsUsed = false,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return (accessToken, refreshTokenString, expiresAt);
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> RefreshTokenAsync(
        string token,
        string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(token);
        if (principal == null)
            throw new SecurityTokenException("Invalid access token.");

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new SecurityTokenException("User identifier missing from token.");

        var existingRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId);

        if (existingRefreshToken == null)
            throw new SecurityTokenException("Refresh token not found.");

        // Security check: If revoked or already used, compromise detected! Revoke entire token family.
        if (existingRefreshToken.IsRevoked || existingRefreshToken.IsUsed || existingRefreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            var userTokens = await _context.RefreshTokens.Where(r => r.UserId == userId).ToListAsync();
            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
            throw new SecurityTokenException("Token compromised or expired. All active sessions revoked for security.");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            throw new SecurityTokenException("User is inactive or deleted.");

        // Mark current token as used
        existingRefreshToken.IsUsed = true;

        // Fetch user roles
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var roles = await _context.Roles
            .Where(r => userRoleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync();

        // Generate new token pair (Rotation)
        var result = await GenerateTokensAsync(user, roles);

        existingRefreshToken.ReplacedByToken = result.RefreshToken;
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string userId)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId);

        if (token == null || !token.IsActive)
            return false;

        token.IsRevoked = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? "ApexCMMS_SuperSecretKey_ProductionGrade_2026_Minimum32CharsLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "ApexCMMS";
        var audience = _configuration["JwtSettings:Audience"] ?? "ApexCMMS-FieldApp";

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = false // Ignore expiration for refreshing
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
