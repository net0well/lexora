using Lexora.Application.Common.Paging;
using Lexora.Application.Conversations.Abstractions;
using Lexora.Application.Conversations.GetConversation;
using Lexora.Application.Conversations.ListConversations;
using Lexora.Application.Conversations.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Queries;

public sealed class ConversationQueries(LexoraDbContext context) : IConversationQueries
{
    public async Task<GetConversationResponse?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var conversation = await context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Messages.OrderBy(message => message.CreatedAt).ThenBy(message => message.Id))
            .FirstOrDefaultAsync(conversation => conversation.Id == id && conversation.UserId == userId, cancellationToken);

        return conversation is null ? null : ConversationMapper.ToResponse(conversation);
    }

    public async Task<PagedResponse<ConversationListItemResponse>> ListAsync(ListConversationsQuery query, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var conversations = context.Conversations.AsNoTracking().Where(conversation => conversation.UserId == userId);

        if (query.KnowledgeBaseId is { } knowledgeBaseId)
        {
            conversations = conversations.Where(conversation => conversation.KnowledgeBaseId == knowledgeBaseId);
        }

        var paging = query.Paging;
        var totalItems = await conversations.CountAsync(cancellationToken);
        var items = await conversations
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .ThenByDescending(conversation => conversation.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(conversation => new ConversationListItemResponse(
                conversation.Id,
                conversation.KnowledgeBaseId,
                conversation.Title,
                conversation.Messages.Count,
                conversation.CreatedAt,
                conversation.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResponse.Create<ConversationListItemResponse>(items, paging, totalItems);
    }
}
