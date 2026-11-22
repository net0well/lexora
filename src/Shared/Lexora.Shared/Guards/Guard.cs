using System.Numerics;
using System.Runtime.CompilerServices;

namespace Lexora.Shared.Guards;

public static class Guard
{
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        return value;
    }

    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        value != Guid.Empty ? value : throw new ArgumentException("The identifier cannot be empty.", parameterName);

    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value;
    }

    public static string MaxLength(string value, int maxLength, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        value.Length <= maxLength
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value.Length, $"The value cannot be longer than {maxLength} characters.");

    public static T NotNegative<T>(T value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : INumberBase<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);

        return value;
    }

    public static T Positive<T>(T value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : INumberBase<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, parameterName);

        return value;
    }

    public static TimeSpan Positive(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(parameterName, value, "The duration must be positive.");

    public static TEnum Defined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(parameterName, value, "The value is not defined.");
}
