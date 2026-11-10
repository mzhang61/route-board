namespace RouteBoard.Api.Domain.Entities;

public sealed class Trip
{
    public long Id { get; set; }
    public long RouteId { get; set; }
    public Route Route { get; set; } = null!;
    public long VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public DateTimeOffset ScheduledDeparture { get; set; }
    public DateTimeOffset? ObservedDeparture { get; set; }
    public string Status { get; set; } = "scheduled";
    public int DelayMinutes { get; set; }
    public int LastStopOrder { get; set; }
    public long Version { get; set; } = 1;
}
