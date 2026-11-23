using System.ClientModel;
using System.ClientModel.Primitives;
using Lexora.Infrastructure.AI.Options;
using OpenAI;

namespace Lexora.Infrastructure.AI.Providers.OpenAi;

public static class OpenAiClientFactory
{
    // Local OpenAI-compatible servers accept any key, but the SDK refuses an empty one.
    private const string KeyNotRequired = "not-required";

    public static OpenAIClient Create(AiProfileSettings settings, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var options = new OpenAIClientOptions
        {
            NetworkTimeout = timeout,
            // Short retries live in the resilience pipeline and long ones in Wolverine; SDK retries would multiply them.
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
        };

        if (settings.Endpoint is { } endpoint)
        {
            options.Endpoint = endpoint;
        }

        var key = string.IsNullOrWhiteSpace(settings.ApiKey) ? KeyNotRequired : settings.ApiKey;

        return new OpenAIClient(new ApiKeyCredential(key), options);
    }
}
