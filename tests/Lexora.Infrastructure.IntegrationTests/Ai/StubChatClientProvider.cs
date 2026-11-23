using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class StubChatClientProvider(AiProviderKind kind) : IChatClientProvider
{
    public AiProviderKind Kind => kind;

    public List<(string ProfileName, StubChatClient Client)> Created { get; } = [];

    public IChatClient Create(string profileName, AiProfileSettings settings)
    {
        var client = new StubChatClient();
        Created.Add((profileName, client));

        return client;
    }
}
