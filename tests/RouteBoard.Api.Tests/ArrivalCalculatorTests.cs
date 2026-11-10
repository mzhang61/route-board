using RouteBoard.Api.Features.Trips;

namespace RouteBoard.Api.Tests;

public sealed class ArrivalCalculatorTests
{
    [Theory]
    [InlineData(0, 23)]
    [InlineData(4, 27)]
    public void Arrival_matches_hand_prediction(int delay, int expectedMinute)
    {
        var departure = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var arrivals = ArrivalCalculator.Calculate(departure,
            [new RouteLeg(8, 3), new RouteLeg(12, 0)], delay);
        Assert.Equal(departure.AddMinutes(expectedMinute), arrivals[^1]);
    }

    [Fact]
    public void Negative_travel_time_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        ArrivalCalculator.Calculate(DateTimeOffset.UtcNow, [new RouteLeg(-1, 0)], 0));
}
