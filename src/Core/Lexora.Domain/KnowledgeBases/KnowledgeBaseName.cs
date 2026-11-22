using Lexora.Shared.Domain;
using Lexora.Shared.Results;

namespace Lexora.Domain.KnowledgeBases;

public sealed class KnowledgeBaseName : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 120;

    private KnowledgeBaseName(string value) => Value = value;

    public string Value { get; }

    public static Result<KnowledgeBaseName> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<KnowledgeBaseName>(Notification.Validation("knowledgeBase.name.required", "The name is required."));
        }

        if (trimmed.Length < MinLength)
        {
            return Result.Failure<KnowledgeBaseName>(
                Notification.Validation("knowledgeBase.name.tooShort", $"The name must have at least {MinLength} characters."));
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<KnowledgeBaseName>(
                Notification.Validation("knowledgeBase.name.tooLong", $"The name cannot be longer than {MaxLength} characters."));
        }

        return new KnowledgeBaseName(trimmed);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
