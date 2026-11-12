using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RouteBoard.Api.Tests;

public sealed class RateLimitTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Login_and_reference_routing_have_separate_windows()
    {
        using var client = fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        const string query = "?response_type=code&code_challenge=synthetic&state=synthetic";
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var login = await client.GetAsync("/api/auth/authorize" + query);
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.Equal(ApiFixture.Issuer + "/authorize" + query, login.Headers.Location!.AbsoluteUri);
            Assert.True(login.Headers.CacheControl!.NoStore);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/routes/{fixture.RouteId}/estimate")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/auth/authorize" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync($"/api/routes/{fixture.RouteId}/estimate")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/catalog")).StatusCode);
    }
}
