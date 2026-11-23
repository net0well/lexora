using Lexora.Application.Conversations.GetConversation;

namespace Lexora.Application.Conversations.Streaming;

public sealed record AnswerCitationsEvent(IReadOnlyList<CitationResponse> Citations, bool Grounded) : AnswerStreamEvent;
