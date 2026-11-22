using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Documents;

public sealed class ContentHash : ValueObject
{
    public const int Length = 64;

    private ContentHash(string value) => Value = value;

    public string Value { get; }

    public static ContentHash FromHex(string value)
    {
        var normalized = Guard.NotNullOrWhiteSpace(value).ToLowerInvariant();

        return normalized.Length == Length && normalized.All(char.IsAsciiHexDigitLower)
            ? new ContentHash(normalized)
            : throw new ArgumentException($"The content hash must be a {Length}-character SHA-256 hex string.", nameof(value));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
