namespace Lexora.Application.Conversations.Streaming;

public sealed record AnswerDoneEvent(
    Guid ConversationId,
    Guid MessageId,
    string Provider,
    string Model,
    int InputTokens,
    int OutputTokens,
    long LatencyMs) : AnswerStreamEvent;
