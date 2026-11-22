using Lexora.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository(LexoraDbContext context) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Conversations
            .Include(conversation => conversation.Messages.OrderBy(message => message.CreatedAt).ThenBy(message => message.Id))
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken) =>
        await context.Conversations.AddAsync(conversation, cancellationToken);
}
