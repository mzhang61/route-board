using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace RouteBoard.Api.Features.Routing;

public sealed record Coordinate(double Latitude, double Longitude);
public sealed record RouteEstimate(string Status, double? Seconds, double? Meters,
    DateTimeOffset? RetrievedAt, string Source = "Geoapify reference routing; not live traffic or vehicle GPS");

public sealed class RouteEstimateClient(HttpClient http, IMemoryCache cache, IConfiguration configuration,
    TimeProvider clock)
{
    public async Task<RouteEstimate> GetAsync(IReadOnlyList<Coordinate> coordinates, CancellationToken ct)
    {
        if (coordinates.Count is < 2 or > 20 || coordinates.Any(c =>
            !double.IsFinite(c.Latitude) || !double.IsFinite(c.Longitude) ||
            Math.Abs(c.Latitude) > 90 || Math.Abs(c.Longitude) > 180))
            return new("unavailable", null, null, null);

        var waypoints = string.Join('|', coordinates.Select(c =>
            c.Latitude.ToString("R", CultureInfo.InvariantCulture) + "," +
            c.Longitude.ToString("R", CultureInfo.InvariantCulture)));
        var cacheKey = "routing:drive:" + waypoints;
        cache.TryGetValue<RouteEstimate>(cacheKey, out var previous);
        if (previous?.RetrievedAt is { } fetched && clock.GetUtcNow() - fetched < TimeSpan.FromHours(1))
            return previous with { Status = "cached" };
        var apiKey = configuration["Geoapify:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return Fallback(previous);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                // This client has HTTP logging disabled because the provider requires a key in its URL.
                var uri = "https://api.geoapify.com/v1/routing?mode=drive&waypoints=" +
                    Uri.EscapeDataString(waypoints) + "&apiKey=" + Uri.EscapeDataString(apiKey);
                using var response = await http.GetAsync(uri, deadline.Token);
                if ((int)response.StatusCode == 429) break; // Never retry inside a provider quota window.
                if ((int)response.StatusCode >= 500) continue;
                if (!response.IsSuccessStatusCode) break;
                var content = await response.Content.ReadAsStringAsync(deadline.Token);
                var estimate = Parse(content, clock.GetUtcNow());
                if (estimate is null) break;
                cache.Set(cacheKey, estimate, TimeSpan.FromHours(24));
                return estimate;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
            catch (HttpRequestException) { }
            catch (JsonException) { break; }
            if (attempt == 0) await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
        }
        ct.ThrowIfCancellationRequested();
        return Fallback(previous);
    }

    private static RouteEstimate Fallback(RouteEstimate? previous) => previous is null
        ? new("unavailable", null, null, null) : previous with { Status = "stale" };

    private static RouteEstimate? Parse(string content, DateTimeOffset now)
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array ||
            features.GetArrayLength() == 0 || features[0].ValueKind != JsonValueKind.Object ||
            !features[0].TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object ||
            !properties.TryGetProperty("time", out var time) || time.ValueKind != JsonValueKind.Number ||
            !time.TryGetDouble(out var seconds) || !double.IsFinite(seconds) || seconds <= 0 || seconds > 172800 ||
            !properties.TryGetProperty("distance", out var distance) || distance.ValueKind != JsonValueKind.Number ||
            !distance.TryGetDouble(out var meters) || !double.IsFinite(meters) || meters <= 0 || meters > 2000000)
            return null;
        return new("fresh", seconds, meters, now);
    }
}
