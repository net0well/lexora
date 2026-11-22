using Lexora.Domain.KnowledgeBases;
using Lexora.Infrastructure.Persistence.Configurations;
using Lexora.Shared.Guards;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class KnowledgeBaseRepository(LexoraDbContext context) : IKnowledgeBaseRepository
{
    public Task<KnowledgeBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.KnowledgeBases.FirstOrDefaultAsync(knowledgeBase => knowledgeBase.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = Guard.NotNull(name).Trim().ToLowerInvariant();

        return context.KnowledgeBases.AnyAsync(
            knowledgeBase => EF.Property<string>(knowledgeBase, KnowledgeBaseConfiguration.NormalizedNameProperty) == normalizedName,
            cancellationToken);
    }

    public async Task AddAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken) =>
        await context.KnowledgeBases.AddAsync(knowledgeBase, cancellationToken);
}
