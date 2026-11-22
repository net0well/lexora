namespace Lexora.Shared.Results;

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, IEnumerable<Notification> notifications) : base(notifications) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    internal static Result<T> FromValue(T value) => new(value, []);

    internal static Result<T> FromNotifications(IEnumerable<Notification> notifications) => new(default, notifications);

    public static implicit operator Result<T>(T value) => FromValue(value);
}
