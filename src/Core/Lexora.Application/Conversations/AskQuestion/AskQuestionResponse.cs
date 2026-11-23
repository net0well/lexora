using Lexora.Application.Conversations.GetConversation;

namespace Lexora.Application.Conversations.AskQuestion;

public sealed record AskQuestionResponse(
    Guid ConversationId,
    Guid MessageId,
    string Answer,
    IReadOnlyList<CitationResponse> Citations,
    bool Grounded,
    string Provider,
    string Model,
    int InputTokens,
    int OutputTokens,
    long LatencyMs);
