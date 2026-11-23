using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.Fake;

public sealed class FakeChatClientProvider : IChatClientProvider
{
    public AiProviderKind Kind => AiProviderKind.Fake;

    public IChatClient Create(string profileName, AiProfileSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new FakeChatClient(settings.Model);
    }
}
