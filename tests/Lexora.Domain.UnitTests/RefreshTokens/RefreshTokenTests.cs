using Lexora.Domain.RefreshTokens;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Domain.UnitTests.RefreshTokens;

public sealed class RefreshTokenTests
{
    private static readonly Guid _userId = Guid.CreateVersion7();
    private static readonly TimeSpan _lifetime = TimeSpan.FromDays(7);

    private readonly FakeTimeProvider _time = new(TestClock.Now);

    [Fact]
    public void Issue_NewSession_IsActiveInItsOwnFamily()
    {
        var token = Issue("hash-1");

        token.UserId.ShouldBe(_userId);
        token.TokenHash.ShouldBe("hash-1");
        token.FamilyId.ShouldNotBe(Guid.Empty);
        token.FamilyId.ShouldNotBe(token.Id);
        token.ExpiresAt.ShouldBe(_time.GetUtcNow() + _lifetime);
        token.CreatedAt.ShouldBe(_time.GetUtcNow());
        token.IsActive(_time.GetUtcNow()).ShouldBeTrue();
        token.RevokedAt.ShouldBeNull();
        token.ReplacedByTokenId.ShouldBeNull();
    }

    [Fact]
    public void Issue_TwoSessions_UseDifferentFamilies() => Issue("hash-1").FamilyId.ShouldNotBe(Issue("hash-2").FamilyId);

    [Fact]
    public void Issue_ExpirationNotInFuture_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => RefreshToken.Issue(_userId, "hash", _time.GetUtcNow(), _time.GetUtcNow()));

    [Fact]
    public void Issue_BlankHash_Throws() =>
        Should.Throw<ArgumentException>(() => RefreshToken.Issue(_userId, " ", _time.GetUtcNow() + _lifetime, _time.GetUtcNow()));

    [Fact]
    public void Issue_EmptyUser_Throws() =>
        Should.Throw<ArgumentException>(() => RefreshToken.Issue(Guid.Empty, "hash", _time.GetUtcNow() + _lifetime, _time.GetUtcNow()));

    [Fact]
    public void IsActive_JustBeforeExpiration_ReturnsTrue()
    {
        var token = Issue("hash-1");

        _time.Advance(_lifetime - TimeSpan.FromSeconds(1));

        token.IsActive(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_AtExpiration_ReturnsFalse()
    {
        var token = Issue("hash-1");

        _time.Advance(_lifetime);

        token.IsActive(_time.GetUtcNow()).ShouldBeFalse();
    }

    [Fact]
    public void Rotate_ActiveToken_ReturnsReplacementInSameFamily()
    {
        var token = Issue("hash-1");
        _time.Advance(TimeSpan.FromMinutes(10));

        var result = token.Rotate("hash-2", _time.GetUtcNow() + _lifetime, _time.GetUtcNow());

        result.IsSuccess.ShouldBeTrue();
        var replacement = result.Value;
        replacement.Id.ShouldNotBe(token.Id);
        replacement.UserId.ShouldBe(token.UserId);
        replacement.FamilyId.ShouldBe(token.FamilyId);
        replacement.TokenHash.ShouldBe("hash-2");
        replacement.IsActive(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void Rotate_ActiveToken_RevokesItAsRotatedAndLinksReplacement()
    {
        var token = Issue("hash-1");

        var replacement = token.Rotate("hash-2", _time.GetUtcNow() + _lifetime, _time.GetUtcNow()).Value;

        token.IsActive(_time.GetUtcNow()).ShouldBeFalse();
        token.RevokedAt.ShouldBe(_time.GetUtcNow());
        token.RevocationReason.ShouldBe(RefreshTokenRevocationReason.Rotated);
        token.WasRotated.ShouldBeTrue();
        token.ReplacedByTokenId.ShouldBe(replacement.Id);
    }

    [Fact]
    public void Rotate_AlreadyRotatedToken_FailsAndSignalsReuse()
    {
        var token = Issue("hash-1");
        token.Rotate("hash-2", _time.GetUtcNow() + _lifetime, _time.GetUtcNow());

        var reuse = token.Rotate("hash-3", _time.GetUtcNow() + _lifetime, _time.GetUtcNow());

        reuse.Notifications.Single().ShouldSatisfyAllConditions(
            notification => notification.Key.ShouldBe("auth.refreshToken.invalid"),
            notification => notification.Type.ShouldBe(NotificationType.Unauthorized));
        token.WasRotated.ShouldBeTrue();
    }

    [Fact]
    public void Rotate_ExpiredToken_Fails()
    {
        var token = Issue("hash-1");
        _time.Advance(_lifetime);

        var result = token.Rotate("hash-2", _time.GetUtcNow() + _lifetime, _time.GetUtcNow());

        result.Notifications.Single().Key.ShouldBe("auth.refreshToken.invalid");
        token.WasRotated.ShouldBeFalse();
    }

    [Fact]
    public void Rotate_TokenRevokedByLogout_FailsWithoutSignalingReuse()
    {
        var token = Issue("hash-1");
        token.Revoke(RefreshTokenRevocationReason.Logout, _time.GetUtcNow());

        var result = token.Rotate("hash-2", _time.GetUtcNow() + _lifetime, _time.GetUtcNow());

        result.IsFailure.ShouldBeTrue();
        token.WasRotated.ShouldBeFalse();
    }

    [Theory]
    [InlineData(RefreshTokenRevocationReason.Logout)]
    [InlineData(RefreshTokenRevocationReason.PasswordChanged)]
    [InlineData(RefreshTokenRevocationReason.ReuseDetected)]
    [InlineData(RefreshTokenRevocationReason.UserDeactivated)]
    public void Revoke_ActiveToken_RecordsReasonAndTime(RefreshTokenRevocationReason reason)
    {
        var token = Issue("hash-1");

        token.Revoke(reason, _time.GetUtcNow());

        token.RevocationReason.ShouldBe(reason);
        token.RevokedAt.ShouldBe(_time.GetUtcNow());
        token.IsActive(_time.GetUtcNow()).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_AlreadyRotatedToken_KeepsOriginalReason()
    {
        var token = Issue("hash-1");
        var rotatedAt = _time.GetUtcNow();
        token.Rotate("hash-2", rotatedAt + _lifetime, rotatedAt);
        _time.Advance(TimeSpan.FromMinutes(1));

        token.Revoke(RefreshTokenRevocationReason.ReuseDetected, _time.GetUtcNow());

        token.RevocationReason.ShouldBe(RefreshTokenRevocationReason.Rotated);
        token.RevokedAt.ShouldBe(rotatedAt);
    }

    [Fact]
    public void Revoke_UndefinedReason_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Issue("hash-1").Revoke((RefreshTokenRevocationReason)0, _time.GetUtcNow()));

    private RefreshToken Issue(string tokenHash) => RefreshToken.Issue(_userId, tokenHash, _time.GetUtcNow() + _lifetime, _time.GetUtcNow());
}
