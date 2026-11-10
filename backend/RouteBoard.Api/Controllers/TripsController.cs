using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Features.Trips;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/trips")]
public sealed class TripsController(TripService trips) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery, StringLength(100)] string? search,
        [FromQuery, Range(1, long.MaxValue)] long? routeId,
        [FromQuery, Range(1, long.MaxValue)] long? stopId,
        [FromQuery, RegularExpression("^(scheduled|delayed|running|completed|canceled)$")] string? status,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery, Range(1, 10000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (from > to) return Problem(statusCode: 400, title: "The date range is invalid.");
        var query = trips.Query();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Route.Name.Contains(search) || t.Vehicle.Code.Contains(search));
        if (routeId.HasValue) query = query.Where(t => t.RouteId == routeId);
        if (stopId.HasValue) query = query.Where(t => t.Route.RouteStops.Any(s => s.StopId == stopId));
        if (status is not null) query = query.Where(t => t.Status == status);
        if (from.HasValue) query = query.Where(t => t.ScheduledDeparture >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(t => t.ScheduledDeparture <= to.Value.ToUniversalTime());
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(t => t.ScheduledDeparture).ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = new List<TripView>();
        foreach (var trip in rows) items.Add(await trips.ViewAsync(trip, ct));
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var trip = await trips.Query().SingleOrDefaultAsync(t => t.Id == id, ct);
        return trip is null ? NotFound() : Ok(await trips.ViewAsync(trip, ct));
    }

    [HttpPost]
    [Authorize(Policy = "dispatch")]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> Create(CreateTripRequest request, CancellationToken ct)
    {
        var trip = await trips.CreateAsync(request, Actor(), ct);
        return CreatedAtAction(nameof(Get), new { id = trip.Id }, trip);
    }

    [HttpPost("{id:long}/updates")]
    [Authorize(Policy = "dispatch")]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> Update(long id, UpdateTripRequest request, CancellationToken ct) =>
        Ok(await trips.UpdateAsync(id, request, Actor(), ct));

    private string Actor() => $"{User.FindFirstValue("iss")}|{User.FindFirstValue("sub")}";
}
