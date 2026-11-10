namespace RouteBoard.Api.Domain.Entities;

public sealed class VehiclePosition
{
    public long VehicleId { get; set; }
    public long TripId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public long Sequence { get; set; }
    public bool IsSynthetic { get; set; } = true;
}
