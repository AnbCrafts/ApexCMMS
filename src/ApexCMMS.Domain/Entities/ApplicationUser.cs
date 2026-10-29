using Microsoft.AspNetCore.Identity;

namespace ApexCMMS.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public Guid? DepartmentId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigations
    public virtual Department? Department { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<WorkOrder> CreatedWorkOrders { get; set; } = new List<WorkOrder>();
    public virtual ICollection<WorkOrder> ApprovedWorkOrders { get; set; } = new List<WorkOrder>();
    public virtual ICollection<WorkOrder> AssignedWorkOrders { get; set; } = new List<WorkOrder>();
}
