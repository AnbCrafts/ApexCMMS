using ApexCMMS.Domain.Common;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Domain.Entities;

public class WorkOrder : BaseEntity
{
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string CreatedById { get; set; } = string.Empty;
    public string? ApprovedById { get; set; }
    public string? AssignedTechnicianId { get; set; }
    public decimal EstimatedHours { get; set; } = 0.00m;
    public decimal? ActualHours { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Concurrency Token
    public byte[]? RowVersion { get; set; }

    // Navigations
    public virtual Asset? Asset { get; set; }
    public virtual ApplicationUser? CreatedBy { get; set; }
    public virtual ApplicationUser? ApprovedBy { get; set; }
    public virtual ApplicationUser? AssignedTechnician { get; set; }
    public virtual ICollection<WorkOrderChecklistItem> ChecklistItems { get; set; } = new List<WorkOrderChecklistItem>();
    public virtual ICollection<WorkOrderPartAllocation> PartAllocations { get; set; } = new List<WorkOrderPartAllocation>();
}
