namespace Lexora.Infrastructure.AI.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public AiEmbeddingSettings Embedding { get; init; } = new();
}
