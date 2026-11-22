using Lexora.Shared.Domain;
using Lexora.Shared.Results;

namespace Lexora.Domain.Documents;

public sealed class FileName : ValueObject
{
    public const int MaxLength = 255;

    private const string ForbiddenCharacters = "<>:\"/\\|?*";

    private FileName(string value) => Value = value;

    public string Value { get; }

    public static Result<FileName> Create(string? value)
    {
        var sanitized = RemoveForbiddenCharacters(WithoutPath(value ?? string.Empty)).Trim().TrimEnd('.').TrimEnd();

        return sanitized.Length > 0
            ? new FileName(Truncate(sanitized))
            : Result.Failure<FileName>(Notification.Validation("document.fileName.invalid", "The file name is invalid."));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static string WithoutPath(string value) => value[(value.AsSpan().LastIndexOfAny('/', '\\') + 1)..];

    private static string RemoveForbiddenCharacters(string value) =>
        string.Concat(value.Where(character => !char.IsControl(character) && !ForbiddenCharacters.Contains(character, StringComparison.Ordinal)));

    private static string Truncate(string value)
    {
        if (value.Length <= MaxLength)
        {
            return value;
        }

        var extension = Path.GetExtension(value);

        return extension.Length is > 0 and < MaxLength
            ? string.Concat(value.AsSpan(0, MaxLength - extension.Length), extension)
            : value[..MaxLength];
    }
}
