using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.DTOs.SpareParts;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;

namespace ApexCMMS.Web.Controllers;

[Authorize]
public class SparePartsController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SparePartsController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(bool? lowStockOnly, string? search)
    {
        var query = _context.SpareParts.AsQueryable();

        if (lowStockOnly == true)
        {
            query = query.Where(p => p.QuantityOnHand <= p.MinimumStockLevel);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.PartNumber.Contains(search) || p.BinLocation.Contains(search));
        }

        var parts = await query.OrderBy(p => p.PartNumber).ToListAsync();
        ViewBag.LowStockOnly = lowStockOnly;
        ViewBag.Search = search;

        return View(parts);
    }

    [HttpPost]
    [Authorize(Policy = "TechnicianOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Allocate(AllocateSparePartDto model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid part allocation submission.";
            return RedirectToAction("Details", "WorkOrders", new { id = model.WorkOrderId });
        }

        var part = await _context.SpareParts.FindAsync(model.SparePartId);
        var workOrder = await _context.WorkOrders.FindAsync(model.WorkOrderId);

        if (part == null || workOrder == null)
        {
            TempData["ErrorMessage"] = "Spare part or work order not found.";
            return RedirectToAction("Index");
        }

        if (part.QuantityOnHand < model.Quantity)
        {
            TempData["ErrorMessage"] = $"Insufficient stock for {part.Name}. Available: {part.QuantityOnHand}, Requested: {model.Quantity}";
            return RedirectToAction("Details", "WorkOrders", new { id = model.WorkOrderId });
        }

        // Atomic stock deduction
        part.QuantityOnHand -= model.Quantity;

        var allocation = new WorkOrderPartAllocation
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            SparePartId = part.Id,
            QuantityUsed = model.Quantity,
            UnitPriceAtUsage = part.UnitPrice,
            AllocatedAtUtc = DateTime.UtcNow
        };

        _context.WorkOrderPartAllocations.Add(allocation);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Allocated {model.Quantity}x {part.Name} to {workOrder.WorkOrderNumber}.";
        return RedirectToAction("Details", "WorkOrders", new { id = model.WorkOrderId });
    }
}
