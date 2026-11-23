using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Mapping;
using Lexora.Infrastructure.AI.Registry;

namespace Lexora.Infrastructure.AI.Adapters;

public sealed class AiProfileCatalog(ChatClientRegistry registry) : IAiProfileCatalog
{
    public string DefaultChatProfile => registry.DefaultProfile;

    public IReadOnlyList<AiChatProfile> ListChatProfiles() =>
    [
        .. registry.Profiles
            .OrderByDescending(pair => string.Equals(pair.Key, registry.DefaultProfile, StringComparison.OrdinalIgnoreCase))
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new AiChatProfile(
                pair.Key,
                ChatMessageMapper.ProviderNameOf(pair.Value.Provider),
                pair.Value.Model,
                IsSelectable(pair.Key),
                registry.IsAvailable(pair.Key))),
    ];

    public bool IsSelectable(string name) =>
        registry.Profiles.TryGetValue(name, out var profile) && profile.AllowUserSelection && registry.IsAvailable(name);
}
