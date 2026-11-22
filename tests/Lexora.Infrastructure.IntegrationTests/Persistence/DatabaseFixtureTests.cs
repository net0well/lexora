using Lexora.TestKit.Builders;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class DatabaseFixtureTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Migrations_AfterStartup_AreAllApplied()
    {
        await using var context = Database.CreateContext();

        var pending = await context.Database.GetPendingMigrationsAsync(CancellationToken);
        var applied = await context.Database.GetAppliedMigrationsAsync(CancellationToken);

        pending.ShouldBeEmpty();
        applied.ShouldContain(migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResetAsync_AfterDataWasSaved_EmptiesTables()
    {
        await using (var context = Database.CreateContext())
        {
            context.Users.Add(new UserBuilder().Build());
            await context.SaveChangesAsync(CancellationToken);
        }

        await Database.ResetAsync(CancellationToken);

        await using var verification = Database.CreateContext();
        (await verification.Users.CountAsync(CancellationToken)).ShouldBe(0);
    }
}
