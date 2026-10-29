using ApexCMMS.Domain.Common;

namespace ApexCMMS.Domain.Entities;

public class Department : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigations
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
