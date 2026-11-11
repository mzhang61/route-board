using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RouteBoard.Api.Features.Routing;
using RouteBoard.Api.Features.Trips;

namespace RouteBoard.Api.Tests;

public sealed class RoutingOutageTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private sealed class TimeoutHandler : HttpMessageHandler
    {
        public int Attempts { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Attempts++;
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException("The provider should be canceled at the deadline.");
        }
    }

    [Fact]
    public async Task Routing_timeout_preserves_schedule_reads_and_dispatch_writes()
    {
        var provider = new TimeoutHandler();
        using var app = fixture.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Geoapify:ApiKey", "synthetic-test-key");
            builder.ConfigureServices(services => services.AddHttpClient<RouteEstimateClient>()
                .ConfigurePrimaryHttpMessageHandler(() => provider));
        });
        using var client = app.CreateClient();
        var estimate = client.GetFromJsonAsync<RouteEstimate>($"/api/routes/{fixture.RouteId}/estimate");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/trips")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.Token());
        var created = await client.PostAsJsonAsync("/api/trips",
            new CreateTripRequest(fixture.RouteId, fixture.VehicleId, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("unavailable", (await estimate)!.Status);
        Assert.Equal(2, provider.Attempts);
    }
}
