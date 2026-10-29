using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.DTOs.WorkOrders;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Web.Controllers;

[Authorize]
public class WorkOrdersController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkOrdersController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(WorkOrderStatus? status, WorkOrderPriority? priority)
    {
        var query = _context.WorkOrders
            .Include(w => w.Asset)
            .Include(w => w.AssignedTechnician)
            .Include(w => w.CreatedBy)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(w => w.Priority == priority.Value);
        }

        // If FieldTechnician, show assigned work orders by default unless viewing all
        if (_currentUserService.IsInRole("FieldTechnician") && !_currentUserService.IsInRole("OperationsManager") && !_currentUserService.IsInRole("SuperAdmin"))
        {
            query = query.Where(w => w.AssignedTechnicianId == _currentUserService.UserId);
        }

        var workOrders = await query.OrderByDescending(w => w.CreatedAtUtc).ToListAsync();

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentPriority = priority;

        return View(workOrders);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var wo = await _context.WorkOrders
            .Include(w => w.Asset)
                .ThenInclude(a => a!.Department)
            .Include(w => w.CreatedBy)
            .Include(w => w.ApprovedBy)
            .Include(w => w.AssignedTechnician)
            .Include(w => w.ChecklistItems.OrderBy(c => c.StepNumber))
                .ThenInclude(c => c.CompletedBy)
            .Include(w => w.PartAllocations)
                .ThenInclude(pa => pa.SparePart)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (wo == null)
            return NotFound();

        // Populate technicians dropdown for re-assignment
        var technicians = await _userManager.GetUsersInRoleAsync("FieldTechnician");
        ViewBag.Technicians = new SelectList(technicians.Where(t => t.IsActive), "Id", "FullName", wo.AssignedTechnicianId);

        // Populate available spare parts for allocation modal
        ViewBag.SpareParts = await _context.SpareParts.Where(p => p.QuantityOnHand > 0).ToListAsync();

        return View(wo);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCreateDropdownsAsync();
        return View(new CreateWorkOrderDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkOrderDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateDropdownsAsync();
            return View(model);
        }

        var asset = await _context.Assets.FindAsync(model.AssetId);
        if (asset == null || asset.Status == AssetStatus.Decommissioned)
        {
            ModelState.AddModelError("AssetId", "Selected asset is not valid or decommissioned.");
            await PopulateCreateDropdownsAsync();
            return View(model);
        }

        // Generate sequential WorkOrderNumber (WO-YYYY-XXXXX)
        var currentYear = DateTime.UtcNow.Year;
        var prefix = $"WO-{currentYear}-";
        var countThisYear = await _context.WorkOrders.CountAsync(w => w.WorkOrderNumber.StartsWith(prefix));
        var woNumber = $"{prefix}{(countThisYear + 1001):D5}";

        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            WorkOrderNumber = woNumber,
            Title = model.Title,
            Description = model.Description,
            AssetId = model.AssetId,
            Priority = model.Priority,
            Status = WorkOrderStatus.Submitted, // Automatically submitted for approval
            CreatedById = _currentUserService.UserId ?? string.Empty,
            AssignedTechnicianId = model.AssignedTechnicianId,
            EstimatedHours = model.EstimatedHours,
            ScheduledDate = model.ScheduledDate,
            CreatedAtUtc = DateTime.UtcNow
        };

        // If priority is emergency, update asset status to UnderMaintenance
        if (model.Priority == WorkOrderPriority.Emergency)
        {
            asset.Status = AssetStatus.UnderMaintenance;
        }

        // Add default checklist items if provided or standard 3 safety steps
        var checklistSteps = model.InitialChecklistSteps.Count > 0
            ? model.InitialChecklistSteps
            : new List<string>
            {
                "Inspect equipment perimeter & establish safety perimeter",
                "Verify electrical disconnection and apply LOTO tags",
                "Perform scheduled diagnosis and component maintenance",
                "Conduct post-maintenance operational test run"
            };

        int stepNum = 1;
        foreach (var step in checklistSteps)
        {
            workOrder.ChecklistItems.Add(new WorkOrderChecklistItem
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrder.Id,
                StepNumber = stepNum++,
                Description = step,
                IsCompleted = false
            });
        }

        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Work Order {workOrder.WorkOrderNumber} created and submitted successfully.";
        return RedirectToAction(nameof(Details), new { id = workOrder.Id });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var wo = await _context.WorkOrders.FindAsync(id);
        if (wo == null)
            return NotFound();

        if (wo.Status != WorkOrderStatus.Submitted)
        {
            TempData["ErrorMessage"] = "Only submitted work orders can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        wo.Status = WorkOrderStatus.Approved;
        wo.ApprovedById = _currentUserService.UserId;
        wo.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Work Order {wo.WorkOrderNumber} approved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "TechnicianOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, WorkOrderStatus newStatus, decimal? actualHours)
    {
        var wo = await _context.WorkOrders.Include(w => w.Asset).FirstOrDefaultAsync(w => w.Id == id);
        if (wo == null)
            return NotFound();

        wo.Status = newStatus;
        wo.UpdatedAtUtc = DateTime.UtcNow;

        if (actualHours.HasValue)
        {
            wo.ActualHours = actualHours.Value;
        }

        if (newStatus == WorkOrderStatus.Completed)
        {
            wo.CompletedAtUtc = DateTime.UtcNow;
            if (wo.Asset != null && wo.Asset.Status == AssetStatus.UnderMaintenance)
            {
                wo.Asset.Status = AssetStatus.Operational;
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Work Order status updated to {newStatus}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "TechnicianOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleChecklistItem(Guid checklistItemId, Guid workOrderId, bool isCompleted, string? notes)
    {
        var item = await _context.WorkOrderChecklistItems.FindAsync(checklistItemId);
        if (item == null)
            return NotFound();

        item.IsCompleted = isCompleted;
        item.CompletedAtUtc = isCompleted ? DateTime.UtcNow : null;
        item.CompletedById = isCompleted ? _currentUserService.UserId : null;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            item.Notes = notes;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = workOrderId });
    }

    private async Task PopulateCreateDropdownsAsync()
    {
        ViewBag.AssetId = new SelectList(
            await _context.Assets.Where(a => a.Status != AssetStatus.Decommissioned).OrderBy(a => a.Name).ToListAsync(),
            "Id", "Name");

        var technicians = await _userManager.GetUsersInRoleAsync("FieldTechnician");
        ViewBag.AssignedTechnicianId = new SelectList(technicians.Where(t => t.IsActive), "Id", "FullName");
    }
}
