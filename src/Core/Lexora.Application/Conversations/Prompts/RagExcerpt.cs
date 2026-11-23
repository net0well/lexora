using Lexora.Application.Retrieval;

namespace Lexora.Application.Conversations.Prompts;

public sealed record RagExcerpt(int Number, SearchHit Hit);
