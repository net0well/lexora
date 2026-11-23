using Lexora.Domain.Documents;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly List<Document> _documents = [];

    public IReadOnlyList<Document> Documents => _documents;

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_documents.FirstOrDefault(document => document.Id == id && document.DeletedAt is null));

    public Task<bool> ExistsWithHashAsync(Guid knowledgeBaseId, ContentHash hash, CancellationToken cancellationToken) =>
        Task.FromResult(_documents.Any(document =>
            document.KnowledgeBaseId == knowledgeBaseId && document.ContentHash == hash && document.DeletedAt is null));

    public Task<long> SumActiveSizeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_documents.Where(document => document.DeletedAt is null).Sum(document => document.SizeBytes));

    public Task<IReadOnlyList<Document>> ListStalledAsync(DateTimeOffset updatedBefore, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Document>>(
        [
            .. _documents
                .Where(document => document.DeletedAt is null
                    && document.Status is DocumentStatus.Uploaded or DocumentStatus.Extracted or DocumentStatus.Chunked
                    && document.UpdatedAt < updatedBefore)
                .OrderBy(document => document.UpdatedAt)
                .Take(limit),
        ]);

    public Task<IReadOnlySet<Guid>> FindActiveIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<Guid>>(_documents.Where(document => document.DeletedAt is null && ids.Contains(document.Id)).Select(document => document.Id).ToHashSet());

    public Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        _documents.Add(document);

        return Task.CompletedTask;
    }
}
