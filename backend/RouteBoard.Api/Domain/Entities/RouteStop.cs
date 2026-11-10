
namespace RouteBoard.Api.Domain.Entities;

public sealed class RouteStop
{
    public long Id { get; set; }

    public long RouteId { get; set; }

    public long StopId { get; set; }

    public int StopOrder { get; set; }

    public int DwellMinutes { get; set; }

    public int TravelMinutesFromPreviousStop { get; set; }

    public Route Route { get; set; } = null!;

    public Stop Stop { get; set; } = null!;
}
