using ApexCMMS.Domain.Common;

namespace ApexCMMS.Domain.Entities;

public class WorkOrderPartAllocation : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public Guid SparePartId { get; set; }
    public int QuantityUsed { get; set; }
    public decimal UnitPriceAtUsage { get; set; }
    public DateTime AllocatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigations
    public virtual WorkOrder? WorkOrder { get; set; }
    public virtual SparePart? SparePart { get; set; }
}
