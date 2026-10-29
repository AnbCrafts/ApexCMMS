using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApexCMMS.Application.DTOs.Auth;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;

namespace ApexCMMS.Web.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] LoginRequestDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.UsernameOrEmail)
                   ?? await _userManager.FindByNameAsync(model.UsernameOrEmail);

        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials or account disabled." });

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, refreshToken, expiresAt) = await _jwtTokenService.GenerateTokensAsync(user, roles);

        return Ok(new LoginResponseDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var (newAccessToken, newRefreshToken, expiresAt) =
                await _jwtTokenService.RefreshTokenAsync(model.AccessToken, model.RefreshToken);

            return Ok(new
            {
                success = true,
                accessToken = newAccessToken,
                refreshToken = newRefreshToken,
                expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed.");
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] string refreshToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { message = "User not identified." });

        var revoked = await _jwtTokenService.RevokeTokenAsync(refreshToken, userId);
        return Ok(new { success = revoked });
    }
}
