using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ApexCMMS.Application.Interfaces;

namespace ApexCMMS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? UserEmail => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? FullName => User?.FindFirst("FullName")?.Value ?? User?.Identity?.Name;

    public Guid? DepartmentId
    {
        get
        {
            var deptVal = User?.FindFirst("DepartmentId")?.Value;
            return Guid.TryParse(deptVal, out var guid) ? guid : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
