using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController(RouteBoardDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(new
    {
        source = "Synthetic linear interpolation between fixed stops; not real GPS or a road-following model",
        items = await db.VehiclePositions.AsNoTracking().OrderBy(p => p.VehicleId).ToListAsync(ct)
    });
}
