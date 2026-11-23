namespace Lexora.Application.Conversations.Streaming;

public sealed record AnswerDeltaEvent(string Text) : AnswerStreamEvent;
