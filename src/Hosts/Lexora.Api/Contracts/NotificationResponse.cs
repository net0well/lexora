using Lexora.Shared.Results;

namespace Lexora.Api.Contracts;

public sealed record NotificationResponse(string Key, string Message, NotificationType Type)
{
    public static NotificationResponse From(Notification notification) => new(notification.Key, notification.Message, notification.Type);
}
