using System.Security.Claims;
using ApexCMMS.Domain.Entities;

namespace ApexCMMS.Application.Interfaces;

public interface IJwtTokenService
{
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(ApplicationUser user, IList<string> roles);
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> RefreshTokenAsync(string token, string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken, string userId);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
