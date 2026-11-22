using Lexora.Domain.ApiKeys;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class ApiKeyRepository(LexoraDbContext context) : IApiKeyRepository
{
    public Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.ApiKeys.FirstOrDefaultAsync(apiKey => apiKey.Id == id, cancellationToken);

    public Task<ApiKey?> GetByPrefixAsync(string prefix, CancellationToken cancellationToken) =>
        context.ApiKeys.FirstOrDefaultAsync(apiKey => apiKey.Prefix == prefix, cancellationToken);

    public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken) =>
        await context.ApiKeys.AddAsync(apiKey, cancellationToken);
}
