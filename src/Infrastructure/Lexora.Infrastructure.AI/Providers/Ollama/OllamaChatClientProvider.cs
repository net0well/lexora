using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Lexora.Infrastructure.AI.Providers.Ollama;

public sealed class OllamaChatClientProvider : IChatClientProvider
{
    public AiProviderKind Kind => AiProviderKind.Ollama;

    public IChatClient Create(string profileName, AiProfileSettings settings) =>
        new OllamaApiClient(
            ProviderHttpClient.Create(settings.Endpoint, TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiChatSettings.DefaultTimeoutSeconds)),
            settings.Model);
}
