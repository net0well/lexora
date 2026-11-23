namespace Lexora.Infrastructure.AI.Options;

public sealed class AiChatSettings
{
    public const int DefaultTimeoutSeconds = 60;
    public const float DefaultTemperature = 0.1f;
    public const int DefaultMaxOutputTokens = 1024;

    public string DefaultProfile { get; init; } = string.Empty;

    public Dictionary<string, AiProfileSettings> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public AiProfileSettings? Default => Profiles.GetValueOrDefault(DefaultProfile);
}
