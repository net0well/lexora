namespace Lexora.Shared.Results;

public class Result
{
    private readonly List<Notification> _notifications;

    protected Result(IEnumerable<Notification> notifications) => _notifications = [.. notifications];

    public bool IsSuccess => _notifications.Count == 0;

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Notification> Notifications => _notifications;

    public static Result Success() => new([]);

    public static Result Failure(params IEnumerable<Notification> notifications) => new(RequireAny(notifications));

    public static Result<T> Success<T>(T value) => Result<T>.FromValue(value);

    public static Result<T> Failure<T>(params IEnumerable<Notification> notifications) =>
        Result<T>.FromNotifications(RequireAny(notifications));

    private static Notification[] RequireAny(IEnumerable<Notification> notifications)
    {
        Notification[] materialized = [.. notifications];

        return materialized.Length > 0
            ? materialized
            : throw new ArgumentException("A failed result requires at least one notification.", nameof(notifications));
    }
}
