using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(RouteBoardDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task Get(CancellationToken ct)
    {
        var cursorText = Request.Headers["Last-Event-ID"].FirstOrDefault()
            ?? Request.Query["after"].FirstOrDefault() ?? "0";
        if (!long.TryParse(cursorText, out var cursor) || cursor < 0)
        {
            await Results.Problem(statusCode: 400, title: "Invalid event cursor.").ExecuteAsync(HttpContext);
            return;
        }
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var events = await db.TripEvents.AsNoTracking().Where(e => e.Id > cursor)
                    .OrderBy(e => e.Id).Take(100).ToListAsync(ct);
                foreach (var item in events)
                {
                    await Response.WriteAsync($"id: {item.Id}\nevent: trip\ndata: {item.Snapshot}\n\n", ct);
                    cursor = item.Id;
                }
                await Response.WriteAsync(": heartbeat\n\n", ct);
                await Response.Body.FlushAsync(ct);
                if (events.Count < 100) await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The browser disconnected; persisted events remain available for replay.
        }
    }
}
