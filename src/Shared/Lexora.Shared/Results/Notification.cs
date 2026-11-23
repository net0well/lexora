namespace Lexora.Shared.Results;

public sealed record Notification(string Key, string Message, NotificationType Type)
{
    public static Notification Validation(string key, string message) => new(key, message, NotificationType.Validation);

    public static Notification NotFound(string key, string message) => new(key, message, NotificationType.NotFound);

    public static Notification Conflict(string key, string message) => new(key, message, NotificationType.Conflict);

    public static Notification Unauthorized(string key, string message) => new(key, message, NotificationType.Unauthorized);

    public static Notification Forbidden(string key, string message) => new(key, message, NotificationType.Forbidden);

    public static Notification BusinessRule(string key, string message) => new(key, message, NotificationType.BusinessRule);

    public static Notification Unavailable(string key, string message) => new(key, message, NotificationType.Unavailable);
}
