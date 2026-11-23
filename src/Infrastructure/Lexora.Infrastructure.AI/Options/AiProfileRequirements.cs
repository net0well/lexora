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

        if (profile.Provider is AiProviderKind.OpenAI or AiProviderKind.Anthropic && string.IsNullOrWhiteSpace(profile.ApiKey))
        {
            yield return nameof(AiProfileSettings.ApiKey);
        }
    }

    public static bool IsComplete(AiProfileSettings profile) => Enum.IsDefined(profile.Provider) && !MissingSettings(profile).Any();
}
