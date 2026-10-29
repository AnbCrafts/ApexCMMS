using Microsoft.AspNetCore.Identity;

namespace ApexCMMS.Domain.Entities;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
