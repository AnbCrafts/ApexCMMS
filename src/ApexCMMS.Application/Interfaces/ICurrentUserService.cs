namespace ApexCMMS.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    string? FullName { get; }
    Guid? DepartmentId { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}
