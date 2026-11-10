using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(RouteBoardDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var routes = await db.Routes.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Code)
            .Select(r => new { r.Id, r.Code, r.Name }).ToListAsync(ct);
        var stops = await db.Stops.AsNoTracking().OrderBy(s => s.Code)
            .Select(s => new { s.Id, s.Code, s.Name }).ToListAsync(ct);
        var vehicles = await db.Vehicles.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.Code)
            .Select(v => new { v.Id, v.Code, v.Capacity }).ToListAsync(ct);
        return Ok(new { routes, stops, vehicles, isSynthetic = true });
    }
}
