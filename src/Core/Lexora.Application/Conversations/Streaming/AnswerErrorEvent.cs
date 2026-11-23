using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.Streaming;

public sealed record AnswerErrorEvent(IReadOnlyList<Notification> Notifications) : AnswerStreamEvent;
