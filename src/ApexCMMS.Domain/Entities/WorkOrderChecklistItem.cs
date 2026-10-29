using ApexCMMS.Domain.Common;

namespace ApexCMMS.Domain.Entities;

public class WorkOrderChecklistItem : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public int StepNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletedById { get; set; }
    public string? Notes { get; set; }

    // Navigations
    public virtual WorkOrder? WorkOrder { get; set; }
    public virtual ApplicationUser? CompletedBy { get; set; }
}
