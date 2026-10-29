using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.DTOs.WorkOrders;
using ApexCMMS.Application.Interfaces;

namespace ApexCMMS.Web.Controllers.Api;

[ApiController]
[Route("api/v1/technician")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
public class TechnicianApiController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public TechnicianApiController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("work-orders")]
    public async Task<IActionResult> GetMyAssignedWorkOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var workOrders = await _context.WorkOrders
            .Include(w => w.Asset)
            .Include(w => w.ChecklistItems)
            .Where(w => w.AssignedTechnicianId == userId)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.ScheduledDate)
            .Select(w => new
            {
                w.Id,
                w.WorkOrderNumber,
                w.Title,
                w.Description,
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                Asset = new
                {
                    w.Asset!.Id,
                    w.Asset.AssetTag,
                    w.Asset.Name,
                    Status = w.Asset.Status.ToString()
                },
                TotalChecklistSteps = w.ChecklistItems.Count,
                CompletedSteps = w.ChecklistItems.Count(c => c.IsCompleted),
                Checklist = w.ChecklistItems.OrderBy(c => c.StepNumber).Select(c => new
                {
                    c.Id,
                    c.StepNumber,
                    c.Description,
                    c.IsCompleted,
                    c.Notes
                })
            })
            .ToListAsync();

        return Ok(new { success = true, data = workOrders });
    }

    [HttpPost("checklist/{id}/complete")]
    public async Task<IActionResult> CompleteChecklistStep(Guid id, [FromBody] string? notes)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var item = await _context.WorkOrderChecklistItems
            .Include(c => c.WorkOrder)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (item == null)
            return NotFound(new { success = false, message = "Checklist item not found." });

        item.IsCompleted = true;
        item.CompletedAtUtc = DateTime.UtcNow;
        item.CompletedById = userId;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            item.Notes = notes;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Step marked as complete." });
    }
}
