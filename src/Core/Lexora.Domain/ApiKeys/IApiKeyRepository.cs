namespace Lexora.Domain.ApiKeys;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiKey?> GetByPrefixAsync(string prefix, CancellationToken cancellationToken);

    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken);
}
