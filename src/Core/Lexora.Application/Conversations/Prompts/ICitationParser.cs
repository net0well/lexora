using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Prompts;

public interface ICitationParser
{
    IReadOnlyList<Citation> Parse(string answer, IReadOnlyList<RagExcerpt> excerpts);
}
