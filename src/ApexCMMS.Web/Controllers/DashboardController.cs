using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.DTOs.Dashboard;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var metrics = new DashboardMetricsDto
        {
            TotalAssets = await _context.Assets.CountAsync(),
            OperationalAssets = await _context.Assets.CountAsync(a => a.Status == AssetStatus.Operational),
            UnderMaintenanceAssets = await _context.Assets.CountAsync(a => a.Status == AssetStatus.UnderMaintenance),
            DecommissionedAssets = await _context.Assets.CountAsync(a => a.Status == AssetStatus.Decommissioned),

            TotalWorkOrders = await _context.WorkOrders.CountAsync(),
            PendingApprovalWorkOrders = await _context.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Submitted),
            InProgressWorkOrders = await _context.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.InProgress),
            CompletedWorkOrders = await _context.WorkOrders.CountAsync(w => w.Status == WorkOrderStatus.Completed),
            EmergencyWorkOrders = await _context.WorkOrders.CountAsync(w => w.Priority == WorkOrderPriority.Emergency && w.Status != WorkOrderStatus.Completed),

            TotalSpareParts = await _context.SpareParts.CountAsync(),
            LowStockPartsCount = await _context.SpareParts.CountAsync(p => p.QuantityOnHand <= p.MinimumStockLevel),
            TotalInventoryValuation = await _context.SpareParts.SumAsync(p => p.QuantityOnHand * p.UnitPrice)
        };

        // Recent Work Orders for the dashboard table
        var recentWorkOrders = await _context.WorkOrders
            .Include(w => w.Asset)
            .Include(w => w.AssignedTechnician)
            .OrderByDescending(w => w.CreatedAtUtc)
            .Take(5)
            .ToListAsync();

        ViewBag.RecentWorkOrders = recentWorkOrders;

        // Recent Audit logs (visible to Admins/Auditors)
        if (User.IsInRole("SuperAdmin") || User.IsInRole("SafetyAuditor"))
        {
            ViewBag.RecentAuditLogs = await _context.AuditLogs
                .OrderByDescending(l => l.TimestampUtc)
                .Take(5)
                .ToListAsync();
        }

        return View(metrics);
    }

    [HttpGet]
    public IActionResult Architecture()
    {
        return View();
    }
}
