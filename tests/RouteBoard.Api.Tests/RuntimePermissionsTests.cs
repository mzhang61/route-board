using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RouteBoard.Api.Tests;

public sealed class RuntimePermissionsTests
{
    [Fact]
    public async Task Runtime_can_append_audit_but_cannot_mutate_history_or_schema()
    {
        var database = new TemporaryDatabase();
        await database.InitializeAsync();
        var role = "runtime_test_" + Guid.NewGuid().ToString("N");
        try
        {
            await using var context = database.OpenContext();
            await context.Database.MigrateAsync();
            await using var connection = new NpgsqlConnection(database.ConnectionString);
            await connection.OpenAsync();
            await ExecuteAsync(connection, $"CREATE ROLE {role} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE");
            try
            {
                var grants = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "runtime-grants.sql"));
                await ExecuteAsync(connection, grants.Replace("routeboard_runtime", role));
                await ExecuteAsync(connection, $"SET ROLE {role}");
                await ExecuteAsync(connection, """
                    INSERT INTO audit_events ("Actor", "Action", "TripId", "OccurredAt", "Before", "After")
                    VALUES ('synthetic-test', 'permission-test', 0, now(), 'null', '{}')
                    """);
                await ExecuteAsync(connection, "SELECT * FROM routes");
                foreach (var forbidden in new[]
                {
                    "UPDATE audit_events SET \"Action\" = 'tampered'",
                    "DELETE FROM audit_events",
                    "TRUNCATE audit_events",
                    "ALTER TABLE audit_events DISABLE TRIGGER ALL",
                    "CREATE TABLE public.unapproved (id integer)",
                    "DELETE FROM trip_events"
                })
                {
                    var error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, forbidden));
                    Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
                }
            }
            finally
            {
                await ExecuteAsync(connection, "RESET ROLE");
                await ExecuteAsync(connection, $"DROP OWNED BY {role}; DROP ROLE {role}");
            }
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
