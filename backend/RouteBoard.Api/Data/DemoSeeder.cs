using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RouteBoard.Api.Domain.Entities;
using ShuttleRoute = RouteBoard.Api.Domain.Entities.Route;

namespace RouteBoard.Api.Data;

public static class DemoSeeder
{
    public static async Task SeedAsync(RouteBoardDbContext db)
    {
        if (await db.Routes.AnyAsync(r => r.Code == "RB-DEMO")) return;
        var route = new ShuttleRoute { Code = "RB-DEMO", Name = "Synthetic River Loop" };
        route.RouteStops.Add(new RouteStop { StopOrder = 1, DwellMinutes = 3, TravelMinutesFromPreviousStop = 8,
            Stop = new Stop { Code = "RB-DEMO-A", Name = "Demo North", Location = new Point(-87.63, 41.88) { SRID = 4326 } } });
        route.RouteStops.Add(new RouteStop { StopOrder = 2, DwellMinutes = 0, TravelMinutesFromPreviousStop = 12,
            Stop = new Stop { Code = "RB-DEMO-B", Name = "Demo South", Location = new Point(-87.62, 41.87) { SRID = 4326 } } });
        var vehicle = new Vehicle { Code = "RB-SYN-01", Capacity = 20 };
        db.Trips.Add(new Trip { Route = route, Vehicle = vehicle, ScheduledDeparture = DateTimeOffset.UtcNow.AddHours(1) });
        await db.SaveChangesAsync();
    }
}
