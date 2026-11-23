using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Prompts;

public interface IFollowUpQuestionRewriter
{
    Task<string> RewriteAsync(string question, IReadOnlyList<ConversationMessage> history, string? profile, CancellationToken cancellationToken);
}
