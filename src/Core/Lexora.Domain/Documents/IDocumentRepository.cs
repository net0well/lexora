namespace Lexora.Domain.Documents;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsWithHashAsync(Guid knowledgeBaseId, ContentHash hash, CancellationToken cancellationToken);

    Task<long> SumActiveSizeAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> ListStalledAsync(DateTimeOffset updatedBefore, int limit, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> FindActiveIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task AddAsync(Document document, CancellationToken cancellationToken);
}
