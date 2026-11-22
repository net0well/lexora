using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.AI.Options;

public sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var embedding = options.Embedding;
        List<string> failures = [];

        if (embedding.Dimensions <= 0)
        {
            failures.Add("'Ai:Embedding:Dimensions' must be positive.");
        }

        if (embedding.Active is not { } profile)
        {
            failures.Add($"'Ai:Embedding:Profile' must name one of the configured embedding profiles (current: '{embedding.Profile}').");
        }
        else
        {
            failures.AddRange(ProfileFailures($"Ai:Embedding:Profiles:{embedding.Profile}", profile));
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static IEnumerable<string> ProfileFailures(string path, AiProfileSettings profile)
    {
        if (!Enum.IsDefined(profile.Provider))
        {
            yield return $"'{path}:Provider' is not a known provider.";
        }

        if (profile.Provider == AiProviderKind.Anthropic)
        {
            yield return $"'{path}': Anthropic does not offer embeddings.";
        }

        if (string.IsNullOrWhiteSpace(profile.Model))
        {
            yield return $"'{path}:Model' is required.";
        }

        if (profile.Provider is AiProviderKind.Ollama or AiProviderKind.OpenAICompatible && profile.Endpoint is not { IsAbsoluteUri: true })
        {
            yield return $"'{path}:Endpoint' must be an absolute URL.";
        }

        if (profile.Provider == AiProviderKind.OpenAI && string.IsNullOrWhiteSpace(profile.ApiKey))
        {
            yield return $"'{path}:ApiKey' is required.";
        }

        if (profile.MaxBatchSize <= 0)
        {
            yield return $"'{path}:MaxBatchSize' must be positive.";
        }

        if (profile.TimeoutSeconds is <= 0)
        {
            yield return $"'{path}:TimeoutSeconds' must be positive.";
        }
    }
}
