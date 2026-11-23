namespace Lexora.Application.Conversations.AskQuestion;

public sealed record AskQuestionCommand(Guid ConversationId, string Question, string? Profile = null);
