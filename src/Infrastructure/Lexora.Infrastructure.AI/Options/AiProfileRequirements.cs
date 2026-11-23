namespace Lexora.Infrastructure.AI.Options;

public static class AiProfileRequirements
{
    public static IEnumerable<string> MissingSettings(AiProfileSettings profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.Model))
        {
            yield return nameof(AiProfileSettings.Model);
        }

        if (profile.Provider is AiProviderKind.Ollama or AiProviderKind.OpenAICompatible && profile.Endpoint is not { IsAbsoluteUri: true })
        {
            yield return nameof(AiProfileSettings.Endpoint);
        }

        if (RequiresApiKey(profile) && string.IsNullOrWhiteSpace(profile.ApiKey))
        {
            yield return nameof(AiProfileSettings.ApiKey);
        }
    }

    // Hosted OpenAI-compatible services need a key; local servers such as LM Studio opt out explicitly.
    public static bool RequiresApiKey(AiProfileSettings profile) =>
        profile.RequiresApiKey ?? profile.Provider is AiProviderKind.OpenAI or AiProviderKind.Anthropic or AiProviderKind.OpenAICompatible;

    public static bool IsComplete(AiProfileSettings profile) => Enum.IsDefined(profile.Provider) && !MissingSettings(profile).Any();
}
