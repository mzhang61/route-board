
using Microsoft.EntityFrameworkCore;
using RouteBoard.Api.Domain.Entities;
using ShuttleRoute = RouteBoard.Api.Domain.Entities.Route;

namespace RouteBoard.Api.Data;

public sealed class RouteBoardDbContext : DbContext
{
    public RouteBoardDbContext(
        DbContextOptions<RouteBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShuttleRoute> Routes => Set<ShuttleRoute>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripEvent> TripEvents => Set<TripEvent>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<VehiclePosition>(entity =>
        {
            entity.ToTable("vehicle_positions");
            entity.HasKey(p => p.VehicleId);
            entity.Property(p => p.Sequence).IsConcurrencyToken();
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(p => p.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Trip>().WithMany().HasForeignKey(p => p.TripId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("trips", table =>
            {
                table.HasCheckConstraint("ck_trip_delay", "\"DelayMinutes\" BETWEEN 0 AND 1440");
                table.HasCheckConstraint("ck_trip_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_trip_status", "\"Status\" IN ('scheduled','delayed','running','canceled','completed')");
            });
            entity.Property(t => t.Status).HasMaxLength(20);
            entity.Property(t => t.Version).IsConcurrencyToken();
            entity.HasOne(t => t.Route).WithMany().HasForeignKey(t => t.RouteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Vehicle).WithMany().HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.RouteId, t.ScheduledDeparture });
        });
        modelBuilder.Entity<TripEvent>(entity =>
        {
            entity.ToTable("trip_events");
            entity.Property(e => e.Kind).HasMaxLength(30);
            entity.Property(e => e.Snapshot).HasColumnType("jsonb");
            entity.HasOne(e => e.Trip).WithMany().HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TripId, e.Id });
        });
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.Property(e => e.Actor).HasMaxLength(500);
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Before).HasColumnType("jsonb");
            entity.Property(e => e.After).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TripId, e.Id });
        });

        // Routes
        modelBuilder.Entity<ShuttleRoute>(entity =>
        {
            entity.ToTable("routes");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Code)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(r => r.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(r => r.Code)
                .IsUnique();
        });

        // Stops
        modelBuilder.Entity<Stop>(entity =>
        {
            entity.ToTable("stops");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Code)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(s => s.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(s => s.Location)
                .HasColumnType("geography(Point,4326)")
                .IsRequired();

            entity.HasIndex(s => s.Code)
                .IsUnique();

            entity.HasIndex(s => s.Location)
                .HasMethod("gist");
        });

        // RouteStops
        modelBuilder.Entity<RouteStop>(entity =>
        {
            entity.ToTable("route_stops");

            entity.HasKey(rs => rs.Id);

            entity.HasOne(rs => rs.Route)
                .WithMany(r => r.RouteStops)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rs => rs.Stop)
                .WithMany(s => s.RouteStops)
                .HasForeignKey(rs => rs.StopId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(rs => new
            {
                rs.RouteId,
                rs.StopOrder
            }).IsUnique();

            entity.HasIndex(rs => new
            {
                rs.RouteId,
                rs.StopId
            }).IsUnique();
        });

        // Vehicles
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");

            entity.HasKey(v => v.Id);

            entity.Property(v => v.Code)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(v => v.Code)
                .IsUnique();
        });
    }
}
