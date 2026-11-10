namespace RouteBoard.Api.Domain.Entities;

public sealed class TripEvent
{
    public long Id { get; set; }
    public long TripId { get; set; }
    public Trip Trip { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public int? StopOrder { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Snapshot { get; set; } = "{}";
}
