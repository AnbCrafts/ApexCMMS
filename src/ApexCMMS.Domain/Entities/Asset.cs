using ApexCMMS.Domain.Common;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Domain.Entities;

public class Asset : BaseEntity
{
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public Guid CategoryId { get; set; }
    public Guid DepartmentId { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Operational;
    public AssetCriticality Criticality { get; set; } = AssetCriticality.Medium;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? WarrantyExpiryDate { get; set; }
    
    // Concurrency Token
    public byte[]? RowVersion { get; set; }

    // Navigations
    public virtual AssetCategory? Category { get; set; }
    public virtual Department? Department { get; set; }
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
