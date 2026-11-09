
using Microsoft.EntityFrameworkCore;

namespace RouteBoard.Api.Data;

public sealed class RouteBoardDbContext : DbContext
{
    public RouteBoardDbContext(
        DbContextOptions<RouteBoardDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");
    }
}
