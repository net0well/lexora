using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class StubChatClientProvider(AiProviderKind kind) : IChatClientProvider
{
    private readonly Dictionary<string, StubChatClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public AiProviderKind Kind => kind;

    public List<(string ProfileName, StubChatClient Client)> Created { get; } = [];

    public StubChatClient ClientFor(string profileName)
    {
        if (!_clients.TryGetValue(profileName, out var client))
        {
            client = new StubChatClient();
            _clients[profileName] = client;
        }

        return client;
    }

    public IChatClient Create(string profileName, AiProfileSettings settings)
    {
        var client = ClientFor(profileName);
        Created.Add((profileName, client));

        return client;
    }
}
