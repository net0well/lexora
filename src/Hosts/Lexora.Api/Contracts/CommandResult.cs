using Lexora.Shared.Results;

namespace Lexora.Api.Contracts;

public sealed record CommandResult(bool Success, object? Data, IReadOnlyList<NotificationResponse> Notifications)
{
    public static CommandResult Ok() => new(true, null, []);

    public static CommandResult<T> Ok<T>(T data) => new(true, data, []);

    public static CommandResult Fail(IEnumerable<Notification> notifications) =>
        new(false, null, [.. notifications.Select(NotificationResponse.From)]);
}
