using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.OpenAi;

public sealed class OpenAiChatClientProvider : IChatClientProvider
{
    public AiProviderKind Kind => AiProviderKind.OpenAI;

    public IChatClient Create(string profileName, AiProfileSettings settings) =>
        OpenAiClientFactory.Create(settings, TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiChatSettings.DefaultTimeoutSeconds))
            .GetChatClient(settings.Model)
            .AsIChatClient();
}
