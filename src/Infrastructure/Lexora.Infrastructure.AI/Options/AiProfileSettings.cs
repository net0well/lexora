namespace Lexora.Infrastructure.AI.Options;

public sealed class AiProfileSettings
{
    public AiProviderKind Provider { get; init; }

    public string Model { get; init; } = string.Empty;

    public Uri? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public bool? RequiresApiKey { get; init; }

    public int? TimeoutSeconds { get; init; }

    public bool AllowUserSelection { get; init; }

    public float? Temperature { get; init; }

    public int? MaxOutputTokens { get; init; }

    public int MaxBatchSize { get; init; } = 32;

    public string QueryPrefix { get; init; } = string.Empty;

    public string DocumentPrefix { get; init; } = string.Empty;
}
