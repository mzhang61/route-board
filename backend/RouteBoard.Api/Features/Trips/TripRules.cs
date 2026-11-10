using RouteBoard.Api.Domain.Entities;

namespace RouteBoard.Api.Features.Trips;

public sealed class TripRuleException(string message, int statusCode = 409) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public static class TripRules
{
    public static void Apply(Trip trip, UpdateTripRequest update, int finalStopOrder, DateTimeOffset now)
    {
        if (update.DelayMinutes is < 0 or > 1440 || update.Version < 1)
            throw new TripRuleException("Invalid delay or version.", 400);
        if (trip.Version != update.Version)
            throw new TripRuleException("This trip changed. Refresh it before trying again.");
        if (trip.Status is "canceled" or "completed")
            throw new TripRuleException("A finished trip cannot be changed.");

        switch (update.Kind)
        {
            case "depart" when trip.ObservedDeparture is null:
                trip.ObservedDeparture = now;
                trip.Status = "running";
                break;
            case "delay":
                // This is the total delay, not an increment. Replayed snapshots are idempotent.
                trip.DelayMinutes = update.DelayMinutes;
                trip.Status = trip.ObservedDeparture is null ? "delayed" : "running";
                break;
            case "arrive" when trip.ObservedDeparture is not null:
                if (update.StopOrder != trip.LastStopOrder + 1 || update.StopOrder > finalStopOrder)
                    throw new TripRuleException("Record the next ordered stop only.");
                trip.LastStopOrder = update.StopOrder.Value;
                if (trip.LastStopOrder == finalStopOrder)
                    trip.Status = "completed";
                break;
            case "cancel":
                trip.Status = "canceled";
                break;
            default:
                throw new TripRuleException("This update is not valid for the current trip state.");
        }
        trip.Version++;
    }
}
