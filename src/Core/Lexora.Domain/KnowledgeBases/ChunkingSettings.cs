using Lexora.Shared.Domain;
using Lexora.Shared.Results;

namespace Lexora.Domain.KnowledgeBases;

public sealed class ChunkingSettings : ValueObject
{
    public const int MinimumMaxTokens = 200;
    public const int MaximumMaxTokens = 2000;
    public const int DefaultMaxTokens = 800;
    public const int DefaultOverlapTokens = 120;

    private ChunkingSettings(int maxTokens, int overlapTokens)
    {
        MaxTokens = maxTokens;
        OverlapTokens = overlapTokens;
    }

    public static ChunkingSettings Default { get; } = new(DefaultMaxTokens, DefaultOverlapTokens);

    public int MaxTokens { get; }

    public int OverlapTokens { get; }

    public static Result<ChunkingSettings> Create(int maxTokens, int overlapTokens)
    {
        var isValid = maxTokens is >= MinimumMaxTokens and <= MaximumMaxTokens
            && overlapTokens >= 0
            && overlapTokens <= maxTokens / 2;

        return isValid
            ? new ChunkingSettings(maxTokens, overlapTokens)
            : Result.Failure<ChunkingSettings>(Notification.Validation(
                "knowledgeBase.chunking.invalid",
                $"Max tokens must be between {MinimumMaxTokens} and {MaximumMaxTokens}, and overlap between 0 and half of max tokens."));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxTokens;
        yield return OverlapTokens;
    }
}
