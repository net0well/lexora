using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.AI.Options;

public sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [.. ChatFailures(options.Chat), .. EmbeddingFailures(options.Embedding)];

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static IEnumerable<string> ChatFailures(AiChatSettings chat)
    {
        if (chat.Default is null)
        {
            yield return $"'Ai:Chat:DefaultProfile' must name one of the configured chat profiles (current: '{chat.DefaultProfile}').";
        }

        foreach (var (profileName, profile) in chat.Profiles)
        {
            var path = $"Ai:Chat:Profiles:{profileName}";

            foreach (var failure in ShapeFailures(path, profile))
            {
                yield return failure;
            }

            // Only the default profile must be usable; other incomplete profiles are listed as unavailable.
            if (string.Equals(profileName, chat.DefaultProfile, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var failure in CompletenessFailures(path, profile))
                {
                    yield return failure;
                }
            }

            if (profile.Temperature is < 0 or > 2)
            {
                yield return $"'{path}:Temperature' must be between 0 and 2.";
            }

            if (profile.MaxOutputTokens is <= 0)
            {
                yield return $"'{path}:MaxOutputTokens' must be positive.";
            }
        }
    }

    private static IEnumerable<string> EmbeddingFailures(AiEmbeddingSettings embedding)
    {
        if (embedding.Dimensions <= 0)
        {
            yield return "'Ai:Embedding:Dimensions' must be positive.";
        }

        if (embedding.Active is not { } profile)
        {
            yield return $"'Ai:Embedding:Profile' must name one of the configured embedding profiles (current: '{embedding.Profile}').";
            yield break;
        }

        var path = $"Ai:Embedding:Profiles:{embedding.Profile}";

        foreach (var failure in ShapeFailures(path, profile).Concat(CompletenessFailures(path, profile)))
        {
            yield return failure;
        }

        if (profile.Provider == AiProviderKind.Anthropic)
        {
            yield return $"'{path}': Anthropic does not offer embeddings.";
        }

        if (profile.MaxBatchSize <= 0)
        {
            yield return $"'{path}:MaxBatchSize' must be positive.";
        }
    }

    private static IEnumerable<string> ShapeFailures(string path, AiProfileSettings profile)
    {
        if (!Enum.IsDefined(profile.Provider))
        {
            yield return $"'{path}:Provider' is not a known provider.";
        }

        if (profile.TimeoutSeconds is <= 0)
        {
            yield return $"'{path}:TimeoutSeconds' must be positive.";
        }
    }

    private static IEnumerable<string> CompletenessFailures(string path, AiProfileSettings profile) =>
        AiProfileRequirements.MissingSettings(profile).Select(setting => setting == nameof(AiProfileSettings.Endpoint)
            ? $"'{path}:Endpoint' must be an absolute URL."
            : $"'{path}:{setting}' is required.");
}
