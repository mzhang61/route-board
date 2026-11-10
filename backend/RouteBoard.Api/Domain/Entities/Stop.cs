
using NetTopologySuite.Geometries;

namespace RouteBoard.Api.Domain.Entities;

public sealed class Stop
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Point Location { get; set; } = null!;

    public List<RouteStop> RouteStops { get; set; } = new();
}
