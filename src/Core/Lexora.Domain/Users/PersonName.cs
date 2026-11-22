using Lexora.Shared.Domain;
using Lexora.Shared.Results;

namespace Lexora.Domain.Users;

public sealed class PersonName : ValueObject
{
    public const int MinLength = 2;
    public const int MaxLength = 120;

    private PersonName(string value) => Value = value;

    public string Value { get; }

    public static Result<PersonName> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<PersonName>(Notification.Validation("user.name.required", "The name is required."));
        }

        if (trimmed.Length < MinLength)
        {
            return Result.Failure<PersonName>(
                Notification.Validation("user.name.tooShort", $"The name must have at least {MinLength} characters."));
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<PersonName>(
                Notification.Validation("user.name.tooLong", $"The name cannot be longer than {MaxLength} characters."));
        }

        return new PersonName(trimmed);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
