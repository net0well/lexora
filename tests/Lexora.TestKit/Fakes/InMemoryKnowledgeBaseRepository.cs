using Lexora.Domain.KnowledgeBases;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryKnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly List<KnowledgeBase> _knowledgeBases = [];

    public IReadOnlyList<KnowledgeBase> KnowledgeBases => _knowledgeBases;

    public Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_knowledgeBases.FirstOrDefault(knowledgeBase => knowledgeBase.Id == id));

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(_knowledgeBases.Any(knowledgeBase =>
            string.Equals(knowledgeBase.Name.Value, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken)
    {
        _knowledgeBases.Add(knowledgeBase);

        return Task.CompletedTask;
    }
}
