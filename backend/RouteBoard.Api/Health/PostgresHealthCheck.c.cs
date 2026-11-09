
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RouteBoard.Api.Data;

namespace RouteBoard.Api.Health;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly RouteBoardDbContext _dbContext;

    public PostgresHealthCheck(RouteBoardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool connected = await _dbContext.Database
                .CanConnectAsync(cancellationToken);

            return connected
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
