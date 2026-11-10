using Microsoft.EntityFrameworkCore;
using Npgsql;
using RouteBoard.Api.Data;

namespace RouteBoard.Api.Tests;

public sealed class TemporaryDatabase : IAsyncLifetime
{
    private readonly string name = "routeboard_test_" + Guid.NewGuid().ToString("N");
    private string adminConnection = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("ROUTEBOARD_TEST_ADMIN_CONNECTION")
            ?? throw new InvalidOperationException("Run scripts/test.sh to configure a disposable PostgreSQL database.");
        var configuration = new NpgsqlConnectionStringBuilder(adminConnection) { Database = name };
        ConnectionString = configuration.ConnectionString;
        await ExecuteAdminAsync($"CREATE DATABASE {name}");
    }

    public RouteBoardDbContext OpenContext() => new(new DbContextOptionsBuilder<RouteBoardDbContext>()
        .UseNpgsql(ConnectionString, postgres => postgres.UseNetTopologySuite()).Options);

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await ExecuteAdminAsync($"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
    }

    private async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
