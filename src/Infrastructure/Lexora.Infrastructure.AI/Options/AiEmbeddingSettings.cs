namespace Lexora.Infrastructure.AI.Options;

public sealed class AiEmbeddingSettings
{
    public const int DefaultTimeoutSeconds = 30;

    public string Profile { get; init; } = string.Empty;

    public int Dimensions { get; init; } = 1024;

    public Dictionary<string, AiProfileSettings> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public AiProfileSettings? Active => Profiles.GetValueOrDefault(Profile);
}
