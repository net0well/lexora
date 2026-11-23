using Lexora.Application.Retrieval;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Prompts;

public interface IRagPromptBuilder
{
    RagPrompt Build(string question, IReadOnlyList<SearchHit> hits, IReadOnlyList<ConversationMessage> history);
}
