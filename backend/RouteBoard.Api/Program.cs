using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using RouteBoard.Api.Features.Simulation;
using ModelContextProtocol.AspNetCore;
using RouteBoard.Api.Features.Mcp;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RouteBoard.Api.Data;
using RouteBoard.Api.Features.Trips;
using RouteBoard.Api.Health;
using RouteBoard.Api.Features.Routing;
using RouteBoard.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
RoutingTelemetry.Configure(builder);
// Trust only the configured ingress address; clients cannot choose their own scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    var proxyAddress = builder.Configuration["ReverseProxy:Address"];
    if (!string.IsNullOrWhiteSpace(proxyAddress))
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Add(IPAddress.Parse(proxyAddress));
        options.ForwardLimit = 1;
    }
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<RouteBoardDbContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("RouteBoard")
        ?? throw new InvalidOperationException("RouteBoard database connection is not configured.");
    options.UseNpgsql(connectionString, postgres => postgres.UseNetTopologySuite());
});
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TripService>();
builder.Services.AddScoped<VehicleSimulation>();
builder.Services.AddHostedService<SimulationWorker>();
builder.Services.AddDataProtection();
builder.Services.AddMcpServer().WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<TripTools>(TripService.Json).WithResources<TripTools>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<RouteEstimateClient>(client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
    client.MaxResponseContentBufferSize = 1024 * 1024;
}).RemoveAllLoggers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.Audience = builder.Configuration["Authentication:Audience"];
    options.MapInboundClaims = false;
    options.IncludeErrorDetails = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        RoleClaimType = builder.Configuration["Authentication:RoleClaimType"] ?? "roles",
        NameClaimType = "sub",
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("dispatch", policy => policy.RequireAuthenticatedUser()
        .RequireRole("dispatcher").RequireClaim("sub").RequireClaim("iss")
        .RequireAssertion(context => context.User.FindAll("scp")
            .Concat(context.User.FindAll("scope"))
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains("trips.write", StringComparer.Ordinal)));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("routing", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddPolicy("writes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
    if (origins.Length > 0) policy.WithOrigins(origins).WithMethods("GET", "POST")
        .WithHeaders("Authorization", "Content-Type", "Last-Event-ID");
}));

var app = builder.Build();
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<RouteBoardDbContext>();
    await database.Database.MigrateAsync();
    Console.WriteLine("Database migrations completed.");
    return;
}
if (args.Contains("--seed-demo"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo seeding is development-only.");
    await using var scope = app.Services.CreateAsyncScope();
    await DemoSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RouteBoardDbContext>());
    Console.WriteLine("Synthetic demo catalog is ready.");
    return;
}
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
    await Results.Problem(statusCode: context.HttpContext.Response.StatusCode)
        .ExecuteAsync(context.HttpContext));
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapMcp("/mcp").RequireAuthorization().RequireRateLimiting("writes");
app.MapHealthChecks("/healthz");
app.Run();

public partial class Program { }
