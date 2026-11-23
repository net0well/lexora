using Lexora.Domain.Conversations;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly List<Conversation> _conversations = [];

    public IReadOnlyList<Conversation> Conversations => _conversations;

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_conversations.FirstOrDefault(conversation => conversation.Id == id));

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        _conversations.Add(conversation);

        return Task.CompletedTask;
    }

    public void Remove(Conversation conversation) => _conversations.Remove(conversation);
}
