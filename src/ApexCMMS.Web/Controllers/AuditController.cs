using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Application.Interfaces;

namespace ApexCMMS.Web.Controllers;

[Authorize(Policy = "AuditorOrAdmin")]
public class AuditController : Controller
{
    private readonly IApplicationDbContext _context;

    public AuditController(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? entityName, string? actionType)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(l => l.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(l => l.Action == actionType);
        }

        var logs = await query
            .OrderByDescending(l => l.TimestampUtc)
            .Take(100)
            .ToListAsync();

        ViewBag.EntityName = entityName;
        ViewBag.ActionType = actionType;

        return View(logs);
    }
}
