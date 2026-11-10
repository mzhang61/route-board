using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;
using RouteBoard.Api.Domain.Entities;

namespace RouteBoard.Api.Features.Trips;

public sealed class TripService(RouteBoardDbContext db, TimeProvider clock)
{
    public static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    public IQueryable<Trip> Query() => db.Trips.AsNoTracking()
        .Include(t => t.Route).ThenInclude(r => r.RouteStops).ThenInclude(s => s.Stop)
        .Include(t => t.Vehicle);

    public async Task<TripView> ViewAsync(Trip trip, CancellationToken cancellationToken)
    {
        var stops = trip.Route.RouteStops.OrderBy(s => s.StopOrder).ToList();
        var legs = stops.Select(s => new RouteLeg(s.TravelMinutesFromPreviousStop, s.DwellMinutes)).ToList();
        var scheduled = ArrivalCalculator.Calculate(trip.ScheduledDeparture, legs, 0);
        var estimated = ArrivalCalculator.Calculate(
            trip.ObservedDeparture ?? trip.ScheduledDeparture, legs, trip.DelayMinutes);
        var observed = await db.TripEvents.AsNoTracking()
            .Where(e => e.TripId == trip.Id && e.Kind == "arrive")
            .ToDictionaryAsync(e => e.StopOrder!.Value, e => e.OccurredAt, cancellationToken);
        var arrivals = stops.Select((stop, index) => new StopArrival(
            stop.StopId, stop.Stop.Name, stop.StopOrder, scheduled[index], estimated[index],
            observed.TryGetValue(stop.StopOrder, out var time) ? time : null)).ToList();
        return new TripView(trip.Id, trip.RouteId, trip.Route.Name, trip.VehicleId,
            trip.Vehicle.Code, trip.Status, trip.ScheduledDeparture, trip.ObservedDeparture,
            trip.DelayMinutes, trip.Version, true, arrivals);
    }

    public async Task<TripView> CreateAsync(CreateTripRequest request, string actor, CancellationToken ct)
    {
        if (request.ScheduledDeparture == default || request.ScheduledDeparture.Offset != TimeSpan.Zero)
            throw new TripRuleException("Supply a UTC scheduled departure.", 400);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockEventOrderAsync(ct);
        var route = await db.Routes.Include(r => r.RouteStops).ThenInclude(s => s.Stop)
            .SingleOrDefaultAsync(r => r.Id == request.RouteId && r.IsActive, ct);
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == request.VehicleId && v.IsActive, ct);
        if (route is null || vehicle is null || route.RouteStops.Count == 0)
            throw new TripRuleException("Choose an active route with stops and an active vehicle.", 400);
        var orders = route.RouteStops.Select(s => s.StopOrder).Order().ToArray();
        if (!orders.SequenceEqual(Enumerable.Range(1, orders.Length)))
            throw new TripRuleException("Route stop order must be contiguous and start at one.", 400);
        var trip = new Trip { Route = route, Vehicle = vehicle, ScheduledDeparture = request.ScheduledDeparture };
        db.Trips.Add(trip);
        await db.SaveChangesAsync(ct);
        var view = await ViewAsync(trip, ct);
        Record(trip.Id, "create", actor, "null", view, null, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return view;
    }

    public async Task<TripView> UpdateAsync(long id, UpdateTripRequest request, string actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockEventOrderAsync(ct);
        var trip = await Query().AsTracking().SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new TripRuleException("Trip not found.", 404);
        var before = JsonSerializer.Serialize(await ViewAsync(trip, ct), Json);
        var now = clock.GetUtcNow();
        if (request.Kind == "depart" && await db.Trips.AnyAsync(t => t.Id != id &&
            t.VehicleId == trip.VehicleId && t.Status == "running", ct))
            throw new TripRuleException("This vehicle already has a running trip.");
        TripRules.Apply(trip, request, trip.Route.RouteStops.Max(s => s.StopOrder), now);
        await db.SaveChangesAsync(ct);
        var view = await ViewAsync(trip, ct);
        if (request.Kind == "arrive")
            view = view with { Stops = view.Stops.Select(s => s.Order == request.StopOrder
                ? s with { SimulatedObserved = now } : s).ToList() };
        Record(id, request.Kind, actor, before, view, request.StopOrder, now);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return view;
    }

    private Task<int> LockEventOrderAsync(CancellationToken ct) =>
        // Sequence allocation alone does not guarantee commit order. All event writers take this lock.
        db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7218401)", ct);

    private void Record(long tripId, string action, string actor, string before,
        TripView after, int? stopOrder, DateTimeOffset now)
    {
        var snapshot = JsonSerializer.Serialize(after, Json);
        db.TripEvents.Add(new TripEvent { TripId = tripId, Kind = action,
            StopOrder = action == "arrive" ? stopOrder : null, OccurredAt = now, Snapshot = snapshot });
        db.AuditEvents.Add(new AuditEvent { TripId = tripId, Action = action,
            Actor = actor, Before = before, After = snapshot, OccurredAt = now });
    }
}
