using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.GetConversation;

public sealed record ConversationMessageResponse(
    Guid Id,
    MessageRole Role,
    string Content,
    IReadOnlyList<CitationResponse> Citations,
    string? Provider,
    string? Model,
    int InputTokens,
    int OutputTokens,
    long? LatencyMs,
    DateTimeOffset CreatedAt);
