namespace RouteBoard.Api.Features.Trips;

public sealed record RouteLeg(int TravelMinutes, int DwellMinutes);

public static class ArrivalCalculator
{
    public static IReadOnlyList<DateTimeOffset> Calculate(
        DateTimeOffset departure, IReadOnlyList<RouteLeg> legs, int delayMinutes)
    {
        if (delayMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(delayMinutes));

        var arrivals = new List<DateTimeOffset>(legs.Count);
        var nextArrival = departure.AddMinutes(delayMinutes);
        foreach (var leg in legs)
        {
            if (leg.TravelMinutes < 0 || leg.DwellMinutes < 0)
                throw new ArgumentOutOfRangeException(nameof(legs));

            nextArrival = nextArrival.AddMinutes(leg.TravelMinutes);
            arrivals.Add(nextArrival);
            // Dwell affects the next arrival, never the arrival at this stop.
            nextArrival = nextArrival.AddMinutes(leg.DwellMinutes);
        }
        return arrivals;
    }
}
