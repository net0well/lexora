using Lexora.Shared.Results;

namespace Lexora.Application.Ai;

public static class AiNotifications
{
    public static Notification Unavailable { get; } =
        Notification.Unavailable("ai.unavailable", "The AI provider is unavailable right now. Try again in a few moments.");
}
