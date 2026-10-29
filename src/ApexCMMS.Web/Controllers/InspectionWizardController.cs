using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.DTOs.Inspection;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Web.Controllers;

[Authorize]
public class InspectionWizardController : Controller
{
    private const string SessionKey = "ApexCMMS_InspectionWizard_State";
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public InspectionWizardController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    // Step 1: Asset Selection & Pre-Check Diagnostics
    [HttpGet]
    public async Task<IActionResult> Step1()
    {
        var state = GetOrCreateState();
        state.CurrentStep = 1;
        SaveState(state);

        ViewBag.Assets = new SelectList(
            await _context.Assets.Where(a => a.Status != AssetStatus.Decommissioned).OrderBy(a => a.Name).ToListAsync(),
            "Id", "Name", state.AssetId);

        return View(state);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step1(InspectionWizardState model)
    {
        var state = GetOrCreateState();
        var asset = await _context.Assets.FindAsync(model.AssetId);
        if (asset == null)
        {
            ModelState.AddModelError("AssetId", "Please select a valid equipment.");
            ViewBag.Assets = new SelectList(await _context.Assets.ToListAsync(), "Id", "Name");
            return View(model);
        }

        // Store Step 1 data in session
        state.AssetId = asset.Id;
        state.AssetTag = asset.AssetTag;
        state.AssetName = asset.Name;
        state.OperatingHours = model.OperatingHours;
        state.HasPhysicalVibration = model.HasPhysicalVibration;
        state.HasAbnormalHeat = model.HasAbnormalHeat;
        state.PreCheckNotes = model.PreCheckNotes;
        state.CurrentStep = 2;

        SaveState(state);
        TempData["InfoMessage"] = $"Step 1 completed for {asset.Name}. Proceeding to diagnostics.";
        return RedirectToAction(nameof(Step2));
    }

    // Step 2: Diagnostics & Checklist Definition
    [HttpGet]
    public IActionResult Step2()
    {
        var state = GetOrCreateState();
        if (state.AssetId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Please complete Step 1 first.";
            return RedirectToAction(nameof(Step1));
        }

        state.CurrentStep = 2;
        SaveState(state);
        return View(state);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step2(InspectionWizardState model)
    {
        var state = GetOrCreateState();
        state.WorkOrderTitle = model.WorkOrderTitle;
        state.IssueDescription = model.IssueDescription;
        state.Priority = model.Priority;
        state.EstimatedHours = model.EstimatedHours;
        state.CurrentStep = 3;

        SaveState(state);
        TempData["InfoMessage"] = "Diagnostics recorded. Proceeding to parts reservation and sign-off.";
        return RedirectToAction(nameof(Step3));
    }

    // Step 3: Parts Reservation & Technician Signoff
    [HttpGet]
    public async Task<IActionResult> Step3()
    {
        var state = GetOrCreateState();
        if (state.AssetId == Guid.Empty)
        {
            return RedirectToAction(nameof(Step1));
        }

        state.CurrentStep = 3;
        SaveState(state);

        ViewBag.AvailableParts = await _context.SpareParts.Where(p => p.QuantityOnHand > 0).ToListAsync();
        var technicians = await _userManager.GetUsersInRoleAsync("FieldTechnician");
        ViewBag.Technicians = new SelectList(technicians.Where(t => t.IsActive), "Id", "FullName", state.AssignedTechnicianId);

        return View(state);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPart(Guid sparePartId, int quantity)
    {
        var state = GetOrCreateState();
        var part = await _context.SpareParts.FindAsync(sparePartId);
        if (part != null && quantity > 0 && quantity <= part.QuantityOnHand)
        {
            state.ReservedParts.Add(new ReservedPartItem
            {
                SparePartId = part.Id,
                PartNumber = part.PartNumber,
                PartName = part.Name,
                Quantity = quantity,
                UnitPrice = part.UnitPrice
            });
            SaveState(state);
        }

        return RedirectToAction(nameof(Step3));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFinal(InspectionWizardState model)
    {
        var state = GetOrCreateState();
        if (state.AssetId == Guid.Empty)
        {
            return RedirectToAction(nameof(Step1));
        }

        state.AssignedTechnicianId = model.AssignedTechnicianId;
        state.SignOffBadgeNumber = model.SignOffBadgeNumber;

        // Auto-generate Work Order Number
        var year = DateTime.UtcNow.Year;
        var prefix = $"WO-{year}-";
        var count = await _context.WorkOrders.CountAsync(w => w.WorkOrderNumber.StartsWith(prefix));
        var woNumber = $"{prefix}{(count + 1001):D5}";

        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            WorkOrderNumber = woNumber,
            Title = string.IsNullOrWhiteSpace(state.WorkOrderTitle) ? $"Diagnostic: {state.AssetName}" : state.WorkOrderTitle,
            Description = $"[Diagnostic Findings]: {state.IssueDescription}\n[Pre-Check Notes]: {state.PreCheckNotes}\n[Vibration]: {state.HasPhysicalVibration}, [Heat]: {state.HasAbnormalHeat}",
            AssetId = state.AssetId,
            Priority = state.Priority,
            Status = WorkOrderStatus.Submitted,
            CreatedById = _currentUserService.UserId ?? string.Empty,
            AssignedTechnicianId = state.AssignedTechnicianId,
            EstimatedHours = state.EstimatedHours,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Add Checklist Items from Wizard
        int stepIdx = 1;
        foreach (var task in state.InspectionTasks)
        {
            workOrder.ChecklistItems.Add(new WorkOrderChecklistItem
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrder.Id,
                StepNumber = stepIdx++,
                Description = task,
                IsCompleted = false
            });
        }

        // Add Reserved Parts
        foreach (var part in state.ReservedParts)
        {
            workOrder.PartAllocations.Add(new WorkOrderPartAllocation
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrder.Id,
                SparePartId = part.SparePartId,
                QuantityUsed = part.Quantity,
                UnitPriceAtUsage = part.UnitPrice,
                AllocatedAtUtc = DateTime.UtcNow
            });

            // Deduct inventory
            var dbPart = await _context.SpareParts.FindAsync(part.SparePartId);
            if (dbPart != null && dbPart.QuantityOnHand >= part.Quantity)
            {
                dbPart.QuantityOnHand -= part.Quantity;
            }
        }

        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        // Clear Session state upon successful completion
        HttpContext.Session.Remove(SessionKey);

        TempData["SuccessMessage"] = $"Multi-step inspection wizard completed! Work order {workOrder.WorkOrderNumber} created.";
        return RedirectToAction("Details", "WorkOrders", new { id = workOrder.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        HttpContext.Session.Remove(SessionKey);
        return RedirectToAction(nameof(Step1));
    }

    private InspectionWizardState GetOrCreateState()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new InspectionWizardState();
        }

        return JsonSerializer.Deserialize<InspectionWizardState>(json) ?? new InspectionWizardState();
    }

    private void SaveState(InspectionWizardState state)
    {
        var json = JsonSerializer.Serialize(state);
        HttpContext.Session.SetString(SessionKey, json);
    }
}
