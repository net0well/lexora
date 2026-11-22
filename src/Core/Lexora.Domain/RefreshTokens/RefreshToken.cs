using Lexora.Shared.Domain;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.RefreshTokens;

public sealed class RefreshToken : AggregateRoot
{
    private static readonly Notification _invalid =
        Notification.Unauthorized("auth.refreshToken.invalid", "The session is no longer valid. Sign in again.");

    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, Guid familyId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
        : base(Guid.CreateVersion7())
    {
        UserId = Guard.NotEmpty(userId);
        FamilyId = familyId;
        TokenHash = Guard.NotNullOrWhiteSpace(tokenHash);
        ExpiresAt = expiresAt > now
            ? expiresAt
            : throw new ArgumentOutOfRangeException(nameof(expiresAt), expiresAt, "The expiration must be in the future.");
        CreatedAt = now;
    }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshTokenRevocationReason? RevocationReason { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool WasRotated => RevocationReason == RefreshTokenRevocationReason.Rotated;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now) =>
        new(userId, Guid.CreateVersion7(), tokenHash, expiresAt, now);

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public Result<RefreshToken> Rotate(string replacementTokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (!IsActive(now))
        {
            return Result.Failure<RefreshToken>(_invalid);
        }

        var replacement = new RefreshToken(UserId, FamilyId, replacementTokenHash, expiresAt, now);

        Revoke(RefreshTokenRevocationReason.Rotated, now);
        ReplacedByTokenId = replacement.Id;

        return replacement;
    }

    public void Revoke(RefreshTokenRevocationReason reason, DateTimeOffset now)
    {
        Guard.Defined(reason);

        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevocationReason = reason;
    }
}
