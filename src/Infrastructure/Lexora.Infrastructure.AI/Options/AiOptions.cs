namespace Lexora.Infrastructure.AI.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public AiChatSettings Chat { get; init; } = new();

    public AiEmbeddingSettings Embedding { get; init; } = new();

    public bool LogPrompts { get; init; }
}
