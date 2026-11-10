using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using RouteBoard.Api.Domain.Entities;
using ShuttleRoute = RouteBoard.Api.Domain.Entities.Route;

namespace RouteBoard.Api.Tests;

public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public TemporaryDatabase Database { get; } = new();
    private readonly RSA rsa = RSA.Create(2048);
    public const string Issuer = "https://issuer.example.test";
    public const string Audience = "routeboard-test-api";
    public long RouteId { get; private set; }
    public long VehicleId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:RouteBoard"] = Database.ConnectionString,
                ["Authentication:Authority"] = Issuer,
                ["Authentication:Audience"] = Audience,
                ["Cors:Origins:0"] = "https://board.example.test"
            }));
        builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Only the test host uses this key. Production obtains signing keys from provider metadata.
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(new RsaSecurityKey(rsa) { KeyId = "test-key" });
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                options.TokenValidationParameters.ValidIssuer = Issuer;
            }));
    }

    public async Task InitializeAsync()
    {
        await Database.InitializeAsync();
        await using var db = Database.OpenContext();
        await db.Database.MigrateAsync();
        var route = new ShuttleRoute { Code = "DEMO", Name = "Synthetic River Loop" };
        route.RouteStops.Add(new RouteStop { StopOrder = 1, DwellMinutes = 3,
            TravelMinutesFromPreviousStop = 8,
            Stop = new Stop { Code = "A", Name = "Demo North", Location = new Point(-87.63, 41.88) { SRID = 4326 } } });
        route.RouteStops.Add(new RouteStop { StopOrder = 2, DwellMinutes = 0,
            TravelMinutesFromPreviousStop = 12,
            Stop = new Stop { Code = "B", Name = "Demo South", Location = new Point(-87.62, 41.87) { SRID = 4326 } } });
        var vehicle = new Vehicle { Code = "SYN-01", Capacity = 20 };
        db.Routes.Add(route);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        RouteId = route.Id;
        VehicleId = vehicle.Id;
    }

    public string Token(string role = "dispatcher", string scope = "trips.write", string issuer = Issuer,
        string audience = Audience, bool expired = false, bool wrongKey = false, string subject = "synthetic-test-actor")
    {
        using var alternative = RSA.Create(2048);
        var credentials = new SigningCredentials(
            new RsaSecurityKey(wrongKey ? alternative : rsa) { KeyId = "test-key" }, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim("sub", subject), new Claim("roles", role), new Claim("scp", scope)],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(expired ? -10 : 10), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await Database.DisposeAsync();
        rsa.Dispose();
    }
}
