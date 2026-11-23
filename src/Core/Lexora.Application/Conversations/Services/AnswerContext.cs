using Lexora.Application.Conversations.Prompts;

namespace Lexora.Application.Conversations.Services;

public sealed record AnswerContext(string Question, RagPrompt? Prompt);
