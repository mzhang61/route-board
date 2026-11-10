using System.Security.Claims;
using ModelContextProtocol.Client;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.DataProtection;
using ModelContextProtocol;
using RouteBoard.Api.Features.Mcp;
using RouteBoard.Api.Features.Trips;

namespace RouteBoard.Api.Tests;

public sealed class McpAuthorizationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static ClaimsPrincipal Principal(string scope, string role = "viewer", string subject = "mcp-test") =>
        new(new ClaimsIdentity([new Claim("sub", subject), new Claim("iss", ApiFixture.Issuer),
            new Claim("scp", scope), new Claim("roles", role)], "test", "sub", "roles"));

    [Fact]
    public async Task Transport_checks_scopes_for_read_and_write_tools()
    {
        using var http = fixture.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            fixture.Token("viewer", "mcp.search_trips"));
        await using var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        }, http);
        await using var client = await McpClient.CreateAsync(transport);
        var read = await client.CallToolAsync("search_trips", new Dictionary<string, object?> { ["search"] = "" });
        Assert.NotEqual(true, read.IsError);
        var denied = await client.CallToolAsync("record_trip_update", new Dictionary<string, object?>
        {
            ["confirmationToken"] = "not-a-confirmation", ["confirmed"] = true
        });
        Assert.True(denied.IsError);
    }

    [Fact]
    public async Task Read_scope_cannot_write_or_call_another_read_tool()
    {
        await using var db = fixture.Database.OpenContext();
        var tools = new TripTools(db, new TripService(db, TimeProvider.System), new EphemeralDataProtectionProvider(), TimeProvider.System);
        var user = Principal("mcp.search_trips");
        await tools.SearchTrips(user);
        await Assert.ThrowsAsync<McpException>(() => tools.GetTrip(user, 1));
        await Assert.ThrowsAsync<McpException>(() => tools.RecordUpdate(user, "invalid", true));
    }

    [Fact]
    public async Task Confirmation_is_bound_to_actor_operation_and_version()
    {
        await using var db = fixture.Database.OpenContext();
        var service = new TripService(db, TimeProvider.System);
        var tools = new TripTools(db, service, new EphemeralDataProtectionProvider(), TimeProvider.System);
        var user = Principal("mcp.record_trip_update trips.write", "dispatcher");
        var trip = await service.CreateAsync(new CreateTripRequest(fixture.RouteId, fixture.VehicleId,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero)), "test", default);
        var preview = await tools.PreviewUpdate(user, trip.Id, trip.Version, "delay", 4);
        await Assert.ThrowsAsync<McpException>(() => tools.RecordUpdate(user, preview.ConfirmationToken, false));
        await Assert.ThrowsAsync<McpException>(() => tools.RecordUpdate(
            Principal("mcp.record_trip_update trips.write", "dispatcher", "other"), preview.ConfirmationToken, true));
        var updated = await tools.RecordUpdate(user, preview.ConfirmationToken, true);
        Assert.Equal(4, updated.DelayMinutes);
        await Assert.ThrowsAsync<McpException>(() => tools.RecordUpdate(user, preview.ConfirmationToken, true));
    }

    [Fact]
    public async Task Anonymous_transport_is_rejected()
    {
        using var client = fixture.CreateClient();
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await client.PostAsync("/mcp", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))).StatusCode);
    }
}
