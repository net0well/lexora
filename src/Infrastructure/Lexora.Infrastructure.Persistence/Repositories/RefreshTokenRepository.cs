using Lexora.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(LexoraDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListUnrevokedByFamilyAsync(Guid familyId, CancellationToken cancellationToken) =>
        await context.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListUnrevokedByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public Task<int> DeleteFinishedBeforeAsync(DateTimeOffset before, CancellationToken cancellationToken) =>
        context.RefreshTokens
            .Where(token => token.ExpiresAt < before || token.RevokedAt < before)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken) =>
        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
}
