using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;
using RouteBoard.Api.Domain.Entities;

namespace RouteBoard.Api.Features.Simulation;

public sealed class VehicleSimulation(RouteBoardDbContext db)
{
    public async Task TickAsync(DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // One generator at a time, including when more than one app instance is running.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7218402)", ct);
        var trips = await db.Trips.AsNoTracking().Where(t => t.Status == "running" && t.ObservedDeparture != null)
            .Include(t => t.Route).ThenInclude(r => r.RouteStops).ThenInclude(s => s.Stop).ToListAsync(ct);
        foreach (var trip in trips)
        {
            var stops = trip.Route.RouteStops.OrderBy(s => s.StopOrder).ToList();
            if (stops.Count < 2) continue;
            var position = await db.VehiclePositions.FindAsync([trip.VehicleId], ct);
            if (position is not null && position.ObservedAt >= now) continue;
            var elapsed = Math.Max(0, (now - trip.ObservedDeparture!.Value).TotalMinutes - trip.DelayMinutes);
            var (latitude, longitude) = Interpolate(stops, elapsed);
            if (position is null)
            {
                position = new VehiclePosition { VehicleId = trip.VehicleId };
                db.VehiclePositions.Add(position);
            }
            position.TripId = trip.Id;
            position.Latitude = latitude;
            position.Longitude = longitude;
            position.ObservedAt = now;
            position.Sequence++;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public static (double Latitude, double Longitude) Interpolate(IReadOnlyList<RouteStop> stops, double elapsedMinutes)
    {
        if (stops.Count == 0 || !double.IsFinite(elapsedMinutes) || elapsedMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedMinutes));
        for (var index = 1; index < stops.Count; index++)
        {
            var previous = stops[index - 1];
            var next = stops[index];
            elapsedMinutes -= previous.DwellMinutes;
            if (elapsedMinutes <= 0) return (previous.Stop.Location.Y, previous.Stop.Location.X);
            if (elapsedMinutes < next.TravelMinutesFromPreviousStop)
            {
                var fraction = elapsedMinutes / next.TravelMinutesFromPreviousStop;
                return (
                    previous.Stop.Location.Y + fraction * (next.Stop.Location.Y - previous.Stop.Location.Y),
                    previous.Stop.Location.X + fraction * (next.Stop.Location.X - previous.Stop.Location.X));
            }
            elapsedMinutes -= next.TravelMinutesFromPreviousStop;
        }
        return (stops[^1].Stop.Location.Y, stops[^1].Stop.Location.X);
    }
}

public sealed class SimulationWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<SimulationWorker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("Simulation:Enabled")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<VehicleSimulation>().TickAsync(clock.GetUtcNow(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                // Report the failure category without logging database credentials or provider URLs.
                logger.LogWarning("Synthetic position tick failed: {ErrorType}", error.GetType().Name);
            }
        }
    }
}
