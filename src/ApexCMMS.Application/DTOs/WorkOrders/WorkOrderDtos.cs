using System.ComponentModel.DataAnnotations;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Application.DTOs.WorkOrders;

public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public WorkOrderPriority Priority { get; set; }
    public WorkOrderStatus Status { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public string? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<ChecklistItemDto> ChecklistItems { get; set; } = new();
    public List<AllocatedPartDto> AllocatedParts { get; set; } = new();
}

public class CreateWorkOrderDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Target Asset")]
    public Guid AssetId { get; set; }

    [Required]
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    [Display(Name = "Assign Technician")]
    public string? AssignedTechnicianId { get; set; }

    [Range(0.1, 1000)]
    [Display(Name = "Estimated Hours")]
    public decimal EstimatedHours { get; set; } = 1.0m;

    [DataType(DataType.Date)]
    [Display(Name = "Scheduled Date")]
    public DateTime? ScheduledDate { get; set; }

    public List<string> InitialChecklistSteps { get; set; } = new();
}

public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletedByName { get; set; }
    public string? Notes { get; set; }
}

public class AllocatedPartDto
{
    public Guid Id { get; set; }
    public Guid SparePartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
    public decimal UnitPriceAtUsage { get; set; }
    public decimal TotalCost => QuantityUsed * UnitPriceAtUsage;
}
