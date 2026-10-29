using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.DTOs.Assets;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Web.Controllers;

[Authorize]
public class AssetsController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AssetsController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(string? search, Guid? categoryId, AssetStatus? status)
    {
        var query = _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Department)
            .Include(a => a.WorkOrders)
            .AsQueryable();

        // Department isolation: OperationsManager or Technician only sees their department assets unless SuperAdmin/Auditor
        if (!_currentUserService.IsInRole("SuperAdmin") && !_currentUserService.IsInRole("SafetyAuditor") && _currentUserService.DepartmentId.HasValue)
        {
            query = query.Where(a => a.DepartmentId == _currentUserService.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Name.Contains(search) || a.AssetTag.Contains(search) || (a.SerialNumber != null && a.SerialNumber.Contains(search)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var assets = await query.OrderBy(a => a.Name).ToListAsync();

        ViewBag.Categories = new SelectList(await _context.AssetCategories.ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Search = search;
        ViewBag.CurrentStatus = status;

        return View(assets);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var asset = await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Department)
            .Include(a => a.WorkOrders)
                .ThenInclude(w => w.AssignedTechnician)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null)
            return NotFound();

        return View(asset);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new CreateAssetDto());
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAssetDto model)
    {
        if (await _context.Assets.AnyAsync(a => a.AssetTag == model.AssetTag))
        {
            ModelState.AddModelError("AssetTag", "An equipment with this Asset Tag already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetTag = model.AssetTag.Trim().ToUpper(),
            Name = model.Name,
            Description = model.Description,
            SerialNumber = model.SerialNumber,
            CategoryId = model.CategoryId,
            DepartmentId = model.DepartmentId,
            Status = model.Status,
            Criticality = model.Criticality,
            PurchaseDate = model.PurchaseDate,
            WarrantyExpiryDate = model.WarrantyExpiryDate
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Asset {asset.AssetTag} registered successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        var dto = new EditAssetDto
        {
            Id = asset.Id,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Description = asset.Description,
            SerialNumber = asset.SerialNumber,
            CategoryId = asset.CategoryId,
            DepartmentId = asset.DepartmentId,
            Status = asset.Status,
            Criticality = asset.Criticality,
            PurchaseDate = asset.PurchaseDate,
            WarrantyExpiryDate = asset.WarrantyExpiryDate,
            RowVersion = asset.RowVersion
        };

        await PopulateDropdownsAsync(asset.CategoryId, asset.DepartmentId);
        return View(dto);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditAssetDto model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CategoryId, model.DepartmentId);
            return View(model);
        }

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        // Optimistic Concurrency Check via EF Core ChangeTracker
        try
        {
            asset.Name = model.Name;
            asset.Description = model.Description;
            asset.SerialNumber = model.SerialNumber;
            asset.CategoryId = model.CategoryId;
            asset.DepartmentId = model.DepartmentId;
            asset.Status = model.Status;
            asset.Criticality = model.Criticality;
            asset.PurchaseDate = model.PurchaseDate;
            asset.WarrantyExpiryDate = model.WarrantyExpiryDate;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Asset {asset.AssetTag} updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Another user has updated this equipment simultaneously. Please reload the page to inspect the latest changes.");
            await PopulateDropdownsAsync(model.CategoryId, model.DepartmentId);
            return View(model);
        }
    }

    private async Task PopulateDropdownsAsync(Guid? selectedCategory = null, Guid? selectedDepartment = null)
    {
        ViewBag.CategoryId = new SelectList(await _context.AssetCategories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selectedCategory);
        ViewBag.DepartmentId = new SelectList(await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(), "Id", "Name", selectedDepartment);
    }
}
