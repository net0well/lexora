using Anthropic;
using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.Anthropic;

public sealed class AnthropicChatClientProvider : IChatClientProvider
{
    public AiProviderKind Kind => AiProviderKind.Anthropic;

    public IChatClient Create(string profileName, AiProfileSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiChatSettings.DefaultTimeoutSeconds);

        // Short retries live in the resilience pipeline and long ones in Wolverine; SDK retries would multiply them.
        var client = settings.Endpoint is { } endpoint
            ? new AnthropicClient { ApiKey = settings.ApiKey, Timeout = timeout, MaxRetries = 0, BaseUrl = endpoint.ToString() }
            : new AnthropicClient { ApiKey = settings.ApiKey, Timeout = timeout, MaxRetries = 0 };

        return new ChatClientBuilder(client.AsIChatClient(settings.Model, settings.MaxOutputTokens))
            // Current Claude models reject sampling parameters, so the RAG default temperature must not reach them.
            .ConfigureOptions(options =>
            {
                options.Temperature = null;
                options.TopP = null;
                options.TopK = null;
            })
            .Build();
    }
}
