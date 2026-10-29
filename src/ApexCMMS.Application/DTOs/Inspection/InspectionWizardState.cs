using System.ComponentModel.DataAnnotations;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Application.DTOs.Inspection;

public class InspectionWizardState
{
    public Guid SessionKey { get; set; } = Guid.NewGuid();
    public int CurrentStep { get; set; } = 1;

    // Step 1: Asset & Environmental Diagnostic
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal OperatingHours { get; set; }
    public bool HasPhysicalVibration { get; set; }
    public bool HasAbnormalHeat { get; set; }
    public string PreCheckNotes { get; set; } = string.Empty;

    // Step 2: Maintenance Diagnostic & Checklist
    public string WorkOrderTitle { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public decimal EstimatedHours { get; set; } = 2.0m;
    public List<string> InspectionTasks { get; set; } = new()
    {
        "Perform Lockout/Tagout (LOTO) safety protocol",
        "Inspect electrical contacts and wiring integrity",
        "Check bearing lubrication and rotational resistance",
        "Test emergency shutoff and pressure relief valves"
    };

    // Step 3: Required Spare Parts & Final Dispatch
    public List<ReservedPartItem> ReservedParts { get; set; } = new();
    public string? AssignedTechnicianId { get; set; }
    public string SignOffBadgeNumber { get; set; } = string.Empty;
}

public class ReservedPartItem
{
    public Guid SparePartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
