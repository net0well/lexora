using Lexora.Application.Abstractions.Ai;

namespace Lexora.Application.Conversations.Prompts;

public sealed record RagPrompt(string SystemPrompt, IReadOnlyList<ChatTurn> Turns, IReadOnlyList<RagExcerpt> Excerpts);
