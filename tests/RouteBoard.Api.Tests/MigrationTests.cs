using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RouteBoard.Api.Tests;

public sealed class MigrationTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task Migrations_apply_revert_and_reapply_on_disposable_database()
    {
        var database = new TemporaryDatabase();
        await database.InitializeAsync();
        try
        {
            await using var db = database.OpenContext();
            output.WriteLine("Applying all migrations to an isolated test database.");
            await db.Database.MigrateAsync();
            Assert.True(await db.Database.CanConnectAsync());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            output.WriteLine("Reverting all migrations to 0 on the isolated test database.");
            await db.GetService<IMigrator>().MigrateAsync("0");
            Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(0, await db.Trips.CountAsync());
            output.WriteLine("Reapplied migrations: " + string.Join(", ", await db.Database.GetAppliedMigrationsAsync()));
        }
        finally { await database.DisposeAsync(); }
    }
}
