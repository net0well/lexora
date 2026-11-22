using Lexora.Application.Common.Paging;
using Lexora.Application.KnowledgeBases.Abstractions;
using Lexora.Application.KnowledgeBases.GetKnowledgeBase;
using Lexora.Application.KnowledgeBases.ListKnowledgeBases;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Queries;

public sealed class KnowledgeBaseQueries(LexoraDbContext context) : IKnowledgeBaseQueries
{
    public Task<GetKnowledgeBaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.KnowledgeBases
            .AsNoTracking()
            .Where(knowledgeBase => knowledgeBase.Id == id)
            .Select(knowledgeBase => new GetKnowledgeBaseResponse(
                knowledgeBase.Id,
                knowledgeBase.Name.Value,
                knowledgeBase.Description,
                knowledgeBase.Status,
                knowledgeBase.Chunking.MaxTokens,
                knowledgeBase.Chunking.OverlapTokens,
                context.Documents.Count(document => document.KnowledgeBaseId == knowledgeBase.Id && document.DeletedAt == null),
                knowledgeBase.CreatedAt,
                knowledgeBase.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<KnowledgeBaseListItemResponse>> ListAsync(ListKnowledgeBasesQuery query, CancellationToken cancellationToken)
    {
        var knowledgeBases = context.KnowledgeBases.AsNoTracking();

        if (query.Status is { } status)
        {
            knowledgeBases = knowledgeBases.Where(knowledgeBase => knowledgeBase.Status == status);
        }

        var paging = query.Paging;
        var totalItems = await knowledgeBases.CountAsync(cancellationToken);
        var items = await knowledgeBases
            .OrderBy(knowledgeBase => knowledgeBase.Name)
            .ThenBy(knowledgeBase => knowledgeBase.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(knowledgeBase => new KnowledgeBaseListItemResponse(
                knowledgeBase.Id,
                knowledgeBase.Name.Value,
                knowledgeBase.Description,
                knowledgeBase.Status,
                context.Documents.Count(document => document.KnowledgeBaseId == knowledgeBase.Id && document.DeletedAt == null),
                knowledgeBase.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResponse.Create<KnowledgeBaseListItemResponse>(items, paging, totalItems);
    }
}
