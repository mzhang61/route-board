
var builder = WebApplication.CreateBuilder(args);

// Register MVC controller services.
builder.Services.AddControllers();

// Register application health checks.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Map controller-based API endpoints.
app.MapControllers();

// Health endpoint used by deployment checks.
app.MapHealthChecks("/healthz");

app.Run();
