using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;
using RouteBoard.Api.Infrastructure;
using RouteBoard.Api.Features.Routing;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/routes/{id:long}/estimate")]
[EnableRateLimiting("routing")]
public sealed class RouteEstimatesController(RouteBoardDbContext db, RouteEstimateClient provider, ILoggerFactory logs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        if (!await db.Routes.AnyAsync(r => r.Id == id && r.IsActive, ct)) return NotFound();
        var points = await db.RouteStops.AsNoTracking().Where(s => s.RouteId == id)
            .OrderBy(s => s.StopOrder).Select(s => s.Stop.Location)
            .ToListAsync(ct);
        // Read the geography points before accessing ordinates; PostGIS ST_X/ST_Y require geometry.
        var coordinates = points.Select(point => new Coordinate(point.Y, point.X)).ToArray();
        var estimate = await provider.GetAsync(coordinates, ct);
        RoutingTelemetry.Record(estimate.Status, logs);
        return Ok(estimate);
    }
}
