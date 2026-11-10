using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RouteBoard.Api.Features.Routing;

namespace RouteBoard.Api.Tests;

public sealed class RouteEstimateTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    [Theory]
    [InlineData(503, "{}", "unavailable", 2)]
    [InlineData(429, "{}", "unavailable", 1)]
    [InlineData(200, "{\"features\":[{\"properties\":{\"time\":-1,\"distance\":10}}]}", "unavailable", 1)]
    [InlineData(200, "{\"features\":[{\"properties\":{\"time\":600,\"distance\":2000}}]}", "fresh", 1)]
    public async Task Provider_response_is_validated_and_retries_are_bounded(
        int status, string body, string expectedStatus, int expectedCalls)
    {
        var handler = new StubHandler((HttpStatusCode)status, body);
        using var http = new HttpClient(handler) { MaxResponseContentBufferSize = 1024 * 1024 };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Geoapify:ApiKey"] = "synthetic-test-key" }).Build();
        var client = new RouteEstimateClient(http, cache, config, TimeProvider.System);
        Coordinate[] points = [new(41.88, -87.63), new(41.87, -87.62)];
        var result = await client.GetAsync(points, default);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedCalls, handler.Calls);
        if (expectedStatus == "fresh")
        {
            Assert.Equal(600, result.Seconds);
            Assert.Equal("cached", (await client.GetAsync(points, default)).Status);
            Assert.Equal(1, handler.Calls);
        }
    }
}
