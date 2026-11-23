using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Services;

public interface IAnswerContextBuilder
{
    Task<AnswerContext> BuildAsync(
        Guid knowledgeBaseId,
        string question,
        IReadOnlyList<ConversationMessage> history,
        string? profile,
        CancellationToken cancellationToken);
}
