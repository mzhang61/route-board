using NetTopologySuite.Geometries;
using RouteBoard.Api.Domain.Entities;
using RouteBoard.Api.Features.Simulation;

namespace RouteBoard.Api.Tests;

public sealed class SimulationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(9, 0.5)]
    [InlineData(15, 1)]
    [InlineData(20, 1)]
    public void Position_follows_dwell_then_travel_without_extrapolation(double elapsed, double expected)
    {
        RouteStop[] stops = [
            new() { DwellMinutes = 3, Stop = new Stop { Location = new Point(0, 0) } },
            new() { TravelMinutesFromPreviousStop = 12, Stop = new Stop { Location = new Point(1, 1) } }
        ];
        var position = VehicleSimulation.Interpolate(stops, elapsed);
        Assert.Equal(expected, position.Latitude);
        Assert.Equal(expected, position.Longitude);
    }
}

public sealed class SimulationPersistenceTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Older_ticks_do_not_replace_newer_persisted_positions()
    {
        await using var db = fixture.Database.OpenContext();
        var service = new RouteBoard.Api.Features.Trips.TripService(db, TimeProvider.System);
        var trip = await service.CreateAsync(new(fixture.RouteId, fixture.VehicleId, DateTimeOffset.UtcNow), "test", default);
        trip = await service.UpdateAsync(trip.Id, new(trip.Version, "depart", 0, null), "test", default);
        var simulation = new VehicleSimulation(db);
        var now = trip.SimulatedDeparture!.Value.AddMinutes(9);
        await simulation.TickAsync(now, default);
        var first = await db.VehiclePositions.FindAsync(trip.VehicleId);
        Assert.NotNull(first);
        Assert.Equal(41.875, first.Latitude, 6);
        var sequence = first.Sequence;
        await simulation.TickAsync(now.AddMinutes(-1), default);
        Assert.Equal(sequence, first.Sequence);
        Assert.Equal(now, first.ObservedAt);
        db.ChangeTracker.Clear();
        var persisted = await db.VehiclePositions.FindAsync(trip.VehicleId);
        Assert.Equal(now, persisted!.ObservedAt);
        Assert.True(persisted.IsSynthetic);
    }
}
