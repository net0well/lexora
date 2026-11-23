using Lexora.Domain.RefreshTokens;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class RefreshTokenRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly DateTimeOffset _expiresAt = TestClock.Now.AddDays(7);

    [Fact]
    public async Task GetByHashAsync_RotatedToken_RoundTripsRevocation()
    {
        var user = await SaveUserAsync();
        var original = RefreshToken.Issue(user.Id, "hash-1", _expiresAt, TestClock.Now);
        var replacement = original.Rotate("hash-2", _expiresAt, TestClock.Now).Value;
        await SaveAsync(original, replacement);

        await using var context = Database.CreateContext();
        var loaded = await new RefreshTokenRepository(context).GetByHashAsync("hash-1", CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.UserId.ShouldBe(user.Id);
        loaded.FamilyId.ShouldBe(original.FamilyId);
        loaded.ExpiresAt.ShouldBe(_expiresAt);
        loaded.RevokedAt.ShouldBe(TestClock.Now);
        loaded.RevocationReason.ShouldBe(RefreshTokenRevocationReason.Rotated);
        loaded.ReplacedByTokenId.ShouldBe(replacement.Id);
        loaded.WasRotated.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteFinishedBeforeAsync_Cutoff_RemovesTokensExpiredOrRevokedBeforeIt()
    {
        var user = await SaveUserAsync();
        var expired = RefreshToken.Issue(user.Id, "expired", TestClock.Now.AddDays(-40), TestClock.Now.AddDays(-47));
        var revoked = RefreshToken.Issue(user.Id, "revoked", _expiresAt, TestClock.Now.AddDays(-45));
        revoked.Revoke(RefreshTokenRevocationReason.Logout, TestClock.Now.AddDays(-40));
        var active = RefreshToken.Issue(user.Id, "active", _expiresAt, TestClock.Now);
        await SaveAsync(expired, revoked, active);

        await using (var context = Database.CreateContext())
        {
            (await new RefreshTokenRepository(context).DeleteFinishedBeforeAsync(TestClock.Now.AddDays(-30), CancellationToken)).ShouldBe(2);
        }

        await using var reader = Database.CreateContext();
        (await reader.RefreshTokens.Select(token => token.TokenHash).ToListAsync(CancellationToken)).ShouldBe(["active"]);
    }

    [Fact]
    public async Task GetByHashAsync_UnknownHash_ReturnsNull()
    {
        await using var context = Database.CreateContext();

        (await new RefreshTokenRepository(context).GetByHashAsync("missing", CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ListUnrevokedByFamilyAsync_RotatedFamily_ReturnsOnlyTheActiveToken()
    {
        var user = await SaveUserAsync();
        var original = RefreshToken.Issue(user.Id, "hash-1", _expiresAt, TestClock.Now);
        var replacement = original.Rotate("hash-2", _expiresAt, TestClock.Now).Value;
        var otherSession = RefreshToken.Issue(user.Id, "hash-3", _expiresAt, TestClock.Now);
        await SaveAsync(original, replacement, otherSession);

        await using var context = Database.CreateContext();
        var tokens = await new RefreshTokenRepository(context).ListUnrevokedByFamilyAsync(original.FamilyId, CancellationToken);

        tokens.Select(token => token.Id).ShouldBe([replacement.Id]);
    }

    [Fact]
    public async Task ListUnrevokedByUserAsync_UserWithLoggedOutSession_ReturnsOnlyOpenSessionsOfThatUser()
    {
        var user = await SaveUserAsync("ana.souza@lexora.local");
        var otherUser = await SaveUserAsync("bruno@lexora.local");
        var open = RefreshToken.Issue(user.Id, "hash-open", _expiresAt, TestClock.Now);
        var loggedOut = RefreshToken.Issue(user.Id, "hash-closed", _expiresAt, TestClock.Now);
        loggedOut.Revoke(RefreshTokenRevocationReason.Logout, TestClock.Now);
        await SaveAsync(open, loggedOut, RefreshToken.Issue(otherUser.Id, "hash-other", _expiresAt, TestClock.Now));

        await using var context = Database.CreateContext();
        var tokens = await new RefreshTokenRepository(context).ListUnrevokedByUserAsync(user.Id, CancellationToken);

        tokens.Select(token => token.Id).ShouldBe([open.Id]);
    }
}
