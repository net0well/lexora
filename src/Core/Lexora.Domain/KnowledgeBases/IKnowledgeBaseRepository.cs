namespace Lexora.Domain.KnowledgeBases;

public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken);
}
