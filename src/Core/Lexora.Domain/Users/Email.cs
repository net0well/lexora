using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using Lexora.Shared.Domain;
using Lexora.Shared.Results;

namespace Lexora.Domain.Users;

public sealed class Email : ValueObject
{
    public const int MaxLength = 254;

    private static readonly Notification _invalid = Notification.Validation("user.email.invalid", "The e-mail address is invalid.");

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        return IsWellFormed(normalized) ? new Email(normalized) : Result.Failure<Email>(_invalid);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsWellFormed([NotNullWhen(true)] string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && !value.Any(char.IsWhiteSpace)
        && MailAddress.TryCreate(value, out var address)
        && address.Address == value;
}
