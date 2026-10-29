using ApexCMMS.Domain.Common;

namespace ApexCMMS.Domain.Entities;

public class SparePart : BaseEntity
{
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int QuantityOnHand { get; set; }
    public int MinimumStockLevel { get; set; } = 5;
    public string BinLocation { get; set; } = string.Empty;

    // Concurrency Token
    public byte[]? RowVersion { get; set; }

    // Navigations
    public virtual ICollection<WorkOrderPartAllocation> Allocations { get; set; } = new List<WorkOrderPartAllocation>();
}
