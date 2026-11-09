
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Data;
using RouteBoard.Api.Health;

var builder = WebApplication.CreateBuilder(args);

// Register controller services.
builder.Services.AddControllers();

// Read the connection string from application configuration.
var connectionString =
    builder.Configuration.GetConnectionString("RouteBoard")
    ?? throw new InvalidOperationException(
        "RouteBoard database connection is not configured.");

// Register the database context with dependency injection.
builder.Services.AddDbContext<RouteBoardDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
    )
);

// Register application and database health checks.
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql");

var app = builder.Build();

app.MapControllers();

app.MapHealthChecks("/healthz");

app.Run();
