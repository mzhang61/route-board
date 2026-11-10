using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;
using RouteBoard.Api.Features.Routing;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/routes/{id:long}/estimate")]
[EnableRateLimiting("routing")]
public sealed class RouteEstimatesController(RouteBoardDbContext db, RouteEstimateClient provider) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        if (!await db.Routes.AnyAsync(r => r.Id == id && r.IsActive, ct)) return NotFound();
        var coordinates = await db.RouteStops.AsNoTracking().Where(s => s.RouteId == id)
            .OrderBy(s => s.StopOrder).Select(s => new Coordinate(s.Stop.Location.Y, s.Stop.Location.X))
            .ToListAsync(ct);
        return Ok(await provider.GetAsync(coordinates, ct));
    }
}
