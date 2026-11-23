using System.Collections.Concurrent;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.AI.Registry;

public sealed class ChatClientRegistry(IEnumerable<IChatClientProvider> providers, IOptions<AiOptions> options) : IDisposable
{
    private readonly Dictionary<AiProviderKind, IChatClientProvider> _providers = providers.ToDictionary(provider => provider.Kind);
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _clients = new(StringComparer.OrdinalIgnoreCase);

    public string DefaultProfile => options.Value.Chat.DefaultProfile;

    public IReadOnlyDictionary<string, AiProfileSettings> Profiles => options.Value.Chat.Profiles;

    public bool IsAvailable(string profileName) =>
        Profiles.TryGetValue(profileName, out var profile) && AiProfileRequirements.IsComplete(profile) && _providers.ContainsKey(profile.Provider);

    public ChatProfileClient Resolve(string? profileName)
    {
        var name = string.IsNullOrWhiteSpace(profileName) ? DefaultProfile : profileName;

        if (!Profiles.TryGetValue(name, out var profile))
        {
            throw new InvalidOperationException($"Unknown chat profile '{name}'.");
        }

        if (!IsAvailable(name))
        {
            throw new InvalidOperationException($"Chat profile '{name}' is not available; check its provider, model, endpoint and API key.");
        }

        var client = _clients.GetOrAdd(name, key => new Lazy<IChatClient>(() => _providers[profile.Provider].Create(key, profile))).Value;

        return new ChatProfileClient(name, profile, client);
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values.Where(client => client.IsValueCreated))
        {
            client.Value.Dispose();
        }
    }
}
