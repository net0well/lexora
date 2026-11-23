using Lexora.Shared.Results;

namespace Lexora.Application.Ai;

public static class AiNotifications
{
    public static Notification Unavailable { get; } =
        Notification.Unavailable("ai.unavailable", "The AI provider is unavailable right now. Try again in a few moments.");

    public static Notification ProfileNotSelectable { get; } =
        Notification.Validation("ai.profile.notSelectable", "The chosen AI profile does not exist, is not available or cannot be selected.");
}
