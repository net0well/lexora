namespace Lexora.Api.Contracts;

public sealed record CommandResult<T>(bool Success, T? Data, IReadOnlyList<NotificationResponse> Notifications);
