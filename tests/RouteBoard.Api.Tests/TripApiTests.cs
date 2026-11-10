using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Features.Trips;

namespace RouteBoard.Api.Tests;

public sealed class TripApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient Client(string? token = null)
    {
        var client = fixture.CreateClient();
        if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private CreateTripRequest Request() => new(fixture.RouteId, fixture.VehicleId,
        new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("https://board.example.test", true)]
    [InlineData("https://untrusted.example.test", false)]
    public async Task Cors_allows_only_the_configured_exact_origin(string origin, bool allowed)
    {
        using var client = Client();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/trips");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization");
        var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed) Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Write_requests_are_rate_limited()
    {
        using var client = Client(fixture.Token(subject: "rate-limit-test"));
        var statuses = new List<HttpStatusCode>();
        for (var index = 0; index < 31; index++)
            statuses.Add((await client.PostAsJsonAsync("/api/trips", Request() with { RouteId = 0 })).StatusCode);
        Assert.Equal(30, statuses.Count(status => status == HttpStatusCode.BadRequest));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public async Task Anonymous_write_is_401()
    {
        using var client = Client();
        var response = await client.PostAsJsonAsync("/api/trips", Request());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("viewer", "trips.write")]
    [InlineData("dispatcher", "trips.read")]
    public async Task Missing_role_or_scope_is_403(string role, string scope)
    {
        using var client = Client(fixture.Token(role, scope));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/trips", Request())).StatusCode);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expiry")]
    [InlineData("signature")]
    public async Task Invalid_token_is_401(string failure)
    {
        var token = fixture.Token(issuer: failure == "issuer" ? "https://wrong.example.test" : ApiFixture.Issuer,
            audience: failure == "audience" ? "wrong" : ApiFixture.Audience,
            expired: failure == "expiry", wrongKey: failure == "signature");
        using var client = Client(token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/trips", Request())).StatusCode);
    }

    [Theory]
    [InlineData("/api/trips?page=0")]
    [InlineData("/api/trips?pageSize=101")]
    [InlineData("/api/trips?status=unknown")]
    [InlineData("/api/events?after=-1")]
    public async Task Invalid_query_is_400(string url)
    {
        using var client = Client();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_returns_location_and_concurrent_updates_have_one_winner()
    {
        using var client = Client(fixture.Token());
        var create = await client.PostAsJsonAsync("/api/trips", Request());
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);
        var trip = (await create.Content.ReadFromJsonAsync<TripView>())!;
        Assert.Equal(23, trip.Stops[^1].Estimated.Minute);
        var update = new UpdateTripRequest(trip.Version, "delay", 4, null);
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"/api/trips/{trip.Id}/updates", update),
            client.PostAsJsonAsync($"/api/trips/{trip.Id}/updates", update));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var saved = await client.GetFromJsonAsync<TripView>(create.Headers.Location);
        Assert.Equal(4, saved!.DelayMinutes);
        Assert.Equal(27, saved.Stops[^1].Estimated.Minute);
        await using var db = fixture.Database.OpenContext();
        var audits = await db.AuditEvents.Where(e => e.TripId == trip.Id).OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(2, audits.Count);
        Assert.All(audits, a => Assert.Equal($"{ApiFixture.Issuer}|synthetic-test-actor", a.Actor));
        Assert.Equal(2, await db.TripEvents.CountAsync(e => e.TripId == trip.Id));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE audit_events SET \"Action\" = 'tampered'"));
    }

    [Fact]
    public async Task Out_of_order_arrival_is_rejected_without_audit_or_event()
    {
        using var client = Client(fixture.Token());
        var response = await client.PostAsJsonAsync("/api/trips", Request());
        var trip = (await response.Content.ReadFromJsonAsync<TripView>())!;
        var invalid = await client.PostAsJsonAsync($"/api/trips/{trip.Id}/updates",
            new UpdateTripRequest(1, "arrive", 0, 2));
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
        await using var db = fixture.Database.OpenContext();
        Assert.Equal(1, await db.TripEvents.CountAsync(e => e.TripId == trip.Id));
    }

    [Fact]
    public async Task Replay_starts_after_last_event_id_and_contains_no_actor()
    {
        using var client = Client(fixture.Token());
        var response = await client.PostAsJsonAsync("/api/trips", Request());
        var trip = (await response.Content.ReadFromJsonAsync<TripView>())!;
        await using var db = fixture.Database.OpenContext();
        var cursor = await db.TripEvents.Where(e => e.TripId == trip.Id).Select(e => e.Id).SingleAsync();
        await client.PostAsJsonAsync($"/api/trips/{trip.Id}/updates", new UpdateTripRequest(1, "delay", 4, null));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/events");
        request.Headers.Add("Last-Event-ID", cursor.ToString());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var streamResponse = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        using var reader = new StreamReader(await streamResponse.Content.ReadAsStreamAsync(timeout.Token));
        var idLine = await reader.ReadLineAsync(timeout.Token);
        Assert.True(long.Parse(idLine![4..]) > cursor);
        Assert.Equal("event: trip", await reader.ReadLineAsync(timeout.Token));
        var payload = (await reader.ReadLineAsync(timeout.Token))![6..];
        Assert.DoesNotContain("synthetic-test-actor", payload);
        var snapshot = JsonSerializer.Deserialize<TripView>(payload, TripService.Json)!;
        Assert.Equal(4, snapshot.DelayMinutes);
        Assert.Equal(27, snapshot.Stops[^1].Estimated.Minute);
    }
}
