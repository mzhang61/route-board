using System.ComponentModel;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RouteBoard.Api.Data;
using RouteBoard.Api.Domain.Entities;
using RouteBoard.Api.Features.Trips;

namespace RouteBoard.Api.Features.Mcp;

public sealed record UpdateProposal(long TripId, UpdateTripRequest Update, string Actor, DateTimeOffset ExpiresAt);
public sealed record UpdatePreview(TripView Current, UpdateTripRequest Proposed, string ConfirmationToken,
    DateTimeOffset ExpiresAt, string Notice);

[McpServerToolType]
public sealed class TripTools(RouteBoardDbContext db, TripService trips, IDataProtectionProvider protection,
    TimeProvider clock)
{
    [McpServerTool(Name = "search_trips", ReadOnly = true), Description("Search synthetic trips by route name.")]
    public async Task<IReadOnlyList<TripView>> SearchTrips(ClaimsPrincipal user, string search = "", CancellationToken ct = default)
    {
        await AuthorizeAndAuditAsync(user, "search_trips", false, ct);
        if (search.Length > 100) throw new McpException("Search is too long.");
        var rows = await trips.Query().Where(t => t.Route.Name.Contains(search))
            .OrderBy(t => t.ScheduledDeparture).ThenBy(t => t.Id).Take(20).ToListAsync(ct);
        var results = new List<TripView>();
        foreach (var trip in rows) results.Add(await trips.ViewAsync(trip, ct));
        return results;
    }

    [McpServerTool(Name = "get_trip", ReadOnly = true), Description("Read a synthetic trip timeline.")]
    public async Task<TripView> GetTrip(ClaimsPrincipal user, long tripId, CancellationToken ct = default)
    {
        await AuthorizeAndAuditAsync(user, "get_trip", false, ct);
        return await FindAsync(tripId, ct);
    }

    [McpServerTool(Name = "get_stop_arrivals", ReadOnly = true), Description("Read upcoming synthetic arrivals at a stop.")]
    public async Task<IReadOnlyList<TripView>> GetStopArrivals(ClaimsPrincipal user, long stopId, CancellationToken ct = default)
    {
        await AuthorizeAndAuditAsync(user, "get_stop_arrivals", false, ct);
        var now = clock.GetUtcNow();
        var rows = await trips.Query().Where(t => t.Route.RouteStops.Any(s => s.StopId == stopId)
            && t.Status != "canceled" && t.Status != "completed" && t.ScheduledDeparture >= now.AddDays(-1))
            .OrderBy(t => t.ScheduledDeparture).ThenBy(t => t.Id).Take(20).ToListAsync(ct);
        var results = new List<TripView>();
        foreach (var trip in rows) results.Add(await trips.ViewAsync(trip, ct));
        return results;
    }

    [McpServerTool(Name = "preview_trip_update", ReadOnly = true), Description("Preview an exact update for human confirmation. Does not change the trip.")]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://routeboard/mcp-app.html"}""")]
    public async Task<UpdatePreview> PreviewUpdate(ClaimsPrincipal user, long tripId, long version,
        string kind, int delayMinutes = 0, int? stopOrder = null, CancellationToken ct = default)
    {
        await AuthorizeAndAuditAsync(user, "record_trip_update", true, ct);
        var current = await FindAsync(tripId, ct);
        var update = new UpdateTripRequest(version, kind, delayMinutes, stopOrder);
        var candidate = new Trip { Version = current.Version, Status = current.Status,
            ObservedDeparture = current.SimulatedDeparture, DelayMinutes = current.DelayMinutes,
            LastStopOrder = current.Stops.Where(s => s.SimulatedObserved.HasValue).Select(s => s.Order).DefaultIfEmpty(0).Max() };
        try { TripRules.Apply(candidate, update, current.Stops.Max(s => s.Order), clock.GetUtcNow()); }
        catch (TripRuleException error) { throw new McpException(error.Message); }
        var expiry = clock.GetUtcNow().AddMinutes(5);
        var proposal = new UpdateProposal(tripId, update, Actor(user), expiry);
        var token = protection.CreateProtector("RouteBoard.TripConfirmation.v1")
            .Protect(JsonSerializer.Serialize(proposal, TripService.Json));
        return new(current, update, token, expiry,
            "Ask the user to confirm this exact trip, version, and update. This token expires in five minutes.");
    }

    [McpServerTool(Name = "record_trip_update", Destructive = true), Description("Execute a preview only after explicit confirmation of that exact proposed update.")]
    public async Task<TripView> RecordUpdate(ClaimsPrincipal user, string confirmationToken,
        bool confirmed, CancellationToken ct = default)
    {
        await AuthorizeAndAuditAsync(user, "record_trip_update", true, ct);
        if (!confirmed) throw new McpException("Explicit confirmation is required.");
        if (confirmationToken.Length > 8192) throw new McpException("Invalid confirmation.");
        UpdateProposal proposal;
        try
        {
            var json = protection.CreateProtector("RouteBoard.TripConfirmation.v1").Unprotect(confirmationToken);
            proposal = JsonSerializer.Deserialize<UpdateProposal>(json, TripService.Json)
                ?? throw new JsonException();
        }
        catch (Exception error) when (error is CryptographicException or JsonException)
        { throw new McpException("Invalid confirmation."); }
        if (proposal.Actor != Actor(user) || proposal.ExpiresAt <= clock.GetUtcNow())
            throw new McpException("This confirmation expired or belongs to another actor.");
        try { return await trips.UpdateAsync(proposal.TripId, proposal.Update, Actor(user), ct); }
        catch (TripRuleException error) { throw new McpException(error.Message); }
    }

    [McpServerResource(UriTemplate = "ui://routeboard/mcp-app.html", MimeType = "text/html;profile=mcp-app")]
    public async Task<string> ReadConfirmationApp(ClaimsPrincipal user, CancellationToken ct)
    {
        await AuthorizeAndAuditAsync(user, "record_trip_update", true, ct);
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "mcp-app.html");
        if (!File.Exists(path)) throw new McpException("Build the MCP App before running the server.");
        return await File.ReadAllTextAsync(path, ct);
    }

    private async Task<TripView> FindAsync(long id, CancellationToken ct)
    {
        var trip = await trips.Query().SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new McpException("Trip not found.");
        return await trips.ViewAsync(trip, ct);
    }

    private async Task AuthorizeAndAuditAsync(ClaimsPrincipal user, string tool, bool writes, CancellationToken ct)
    {
        var scopes = user.FindAll("scp").Concat(user.FindAll("scope"))
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToHashSet(StringComparer.Ordinal);
        var allowed = user.Identity?.IsAuthenticated == true && user.HasClaim(c => c.Type == "sub")
            && user.HasClaim(c => c.Type == "iss") && scopes.Contains("mcp." + tool)
            && (writes ? user.IsInRole("dispatcher") && scopes.Contains("trips.write")
                : user.IsInRole("viewer") || user.IsInRole("dispatcher"));
        db.AuditEvents.Add(new AuditEvent { Actor = Actor(user), Action = "mcp." + tool,
            OccurredAt = clock.GetUtcNow(), After = allowed ? "{\"authorized\":true}" : "{\"authorized\":false}" });
        await db.SaveChangesAsync(ct);
        if (!allowed) throw new McpException("This tool requires its explicit scope and role.");
    }

    private static string Actor(ClaimsPrincipal user) => $"{user.FindFirstValue("iss")}|{user.FindFirstValue("sub")}";
}
