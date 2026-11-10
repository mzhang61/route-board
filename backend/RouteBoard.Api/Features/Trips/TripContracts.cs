using System.ComponentModel.DataAnnotations;

namespace RouteBoard.Api.Features.Trips;

public sealed record CreateTripRequest(
    [Range(1, long.MaxValue)] long RouteId,
    [Range(1, long.MaxValue)] long VehicleId,
    DateTimeOffset ScheduledDeparture);

public sealed record UpdateTripRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required, RegularExpression("^(depart|arrive|delay|cancel)$")] string Kind,
    [Range(0, 1440)] int DelayMinutes,
    [Range(1, int.MaxValue)] int? StopOrder);

public sealed record StopArrival(
    long StopId, string Name, int Order,
    DateTimeOffset Scheduled, DateTimeOffset Estimated, DateTimeOffset? SimulatedObserved);

public sealed record TripView(
    long Id, long RouteId, string RouteName, long VehicleId, string VehicleCode,
    string Status, DateTimeOffset ScheduledDeparture, DateTimeOffset? SimulatedDeparture,
    int DelayMinutes, long Version, bool IsSynthetic, IReadOnlyList<StopArrival> Stops);
