using Lexora.Shared.Results;

namespace Lexora.Application.Conversations;

public static class ConversationNotifications
{
    public static Notification NotFound { get; } = Notification.NotFound("conversation.notFound", "Conversation not found.");
}
