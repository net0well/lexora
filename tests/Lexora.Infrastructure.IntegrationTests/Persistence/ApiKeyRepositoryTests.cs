using Lexora.Domain.ApiKeys;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class ApiKeyRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task GetByPrefixAsync_ExistingPrefix_RoundTripsEveryField()
    {
        var admin = await SaveUserAsync(role: UserRole.Admin);
        var apiKey = NewApiKey(admin.Id, "a1b2c3d4", TestClock.Now.AddDays(30));
        apiKey.RegisterUse(TestClock.Now.AddMinutes(1));
        await SaveAsync(apiKey);

        await using var context = Database.CreateContext();
        var loaded = await new ApiKeyRepository(context).GetByPrefixAsync("a1b2c3d4", CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(apiKey.Id);
        loaded.Name.ShouldBe("Reporting agent");
        loaded.KeyHash.ShouldBe(apiKey.KeyHash);
        loaded.Role.ShouldBe(UserRole.Viewer);
        loaded.CreatedBy.ShouldBe(admin.Id);
        loaded.ExpiresAt.ShouldBe(TestClock.Now.AddDays(30));
        loaded.LastUsedAt.ShouldBe(TestClock.Now.AddMinutes(1));
        loaded.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_RevokedKey_ReturnsItWithRevocation()
    {
        var admin = await SaveUserAsync(role: UserRole.Admin);
        var apiKey = NewApiKey(admin.Id, "a1b2c3d4", null);
        apiKey.Revoke(TestClock.Now);
        await SaveAsync(apiKey);

        await using var context = Database.CreateContext();
        var loaded = await new ApiKeyRepository(context).GetByIdAsync(apiKey.Id, CancellationToken);

        loaded.ShouldNotBeNull().RevokedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public async Task GetByPrefixAsync_UnknownPrefix_ReturnsNull()
    {
        await using var context = Database.CreateContext();

        (await new ApiKeyRepository(context).GetByPrefixAsync("zzzzzzzz", CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicatePrefix_IsRejectedByTheDatabase()
    {
        var admin = await SaveUserAsync(role: UserRole.Admin);
        await SaveAsync(NewApiKey(admin.Id, "a1b2c3d4", null));

        await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(NewApiKey(admin.Id, "a1b2c3d4", null)));
    }

    private static ApiKey NewApiKey(Guid createdBy, string prefix, DateTimeOffset? expiresAt) =>
        ApiKey.Create("Reporting agent", prefix, $"hash-{Guid.CreateVersion7():N}", UserRole.Viewer, createdBy, expiresAt, TestClock.Now).Value;
}
