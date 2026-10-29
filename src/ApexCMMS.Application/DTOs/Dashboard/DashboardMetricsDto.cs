namespace ApexCMMS.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    public int TotalAssets { get; set; }
    public int OperationalAssets { get; set; }
    public int UnderMaintenanceAssets { get; set; }
    public int DecommissionedAssets { get; set; }

    public int TotalWorkOrders { get; set; }
    public int PendingApprovalWorkOrders { get; set; }
    public int InProgressWorkOrders { get; set; }
    public int CompletedWorkOrders { get; set; }
    public int EmergencyWorkOrders { get; set; }

    public int TotalSpareParts { get; set; }
    public int LowStockPartsCount { get; set; }

    public decimal TotalInventoryValuation { get; set; }
    public decimal TotalMaintenanceHoursLogged { get; set; }
}
