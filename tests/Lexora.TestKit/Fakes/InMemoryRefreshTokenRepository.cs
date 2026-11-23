using Lexora.Domain.RefreshTokens;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _tokens = [];

    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(_tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> ListUnrevokedByFamilyAsync(Guid familyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>([.. _tokens.Where(token => token.FamilyId == familyId && token.RevokedAt is null)]);

    public Task<IReadOnlyList<RefreshToken>> ListUnrevokedByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>([.. _tokens.Where(token => token.UserId == userId && token.RevokedAt is null)]);

    public Task<int> DeleteFinishedBeforeAsync(DateTimeOffset before, CancellationToken cancellationToken) =>
        Task.FromResult(_tokens.RemoveAll(token => token.ExpiresAt < before || token.RevokedAt < before));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _tokens.Add(refreshToken);

        return Task.CompletedTask;
    }
}
