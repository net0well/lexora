using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.OpenAi;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.OpenAiCompatible;

public sealed class OpenAiCompatibleChatClientProvider : IChatClientProvider
{
    public AiProviderKind Kind => AiProviderKind.OpenAICompatible;

    public IChatClient Create(string profileName, AiProfileSettings settings) =>
        OpenAiClientFactory.Create(settings, TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiChatSettings.DefaultTimeoutSeconds))
            .GetChatClient(settings.Model)
            .AsIChatClient();
}
