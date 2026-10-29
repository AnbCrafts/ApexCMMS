using ApexCMMS.Domain.Common;

namespace ApexCMMS.Domain.Entities;

public class AssetCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigations
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
