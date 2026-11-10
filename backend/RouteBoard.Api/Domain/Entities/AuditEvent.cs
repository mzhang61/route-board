namespace RouteBoard.Api.Domain.Entities;

public sealed class AuditEvent
{
    public long Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public long TripId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Before { get; set; } = "null";
    public string After { get; set; } = "{}";
}
