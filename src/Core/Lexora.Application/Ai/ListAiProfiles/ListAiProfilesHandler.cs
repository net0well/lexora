using Lexora.Application.Abstractions.Ai;
using Lexora.Shared.Results;

namespace Lexora.Application.Ai.ListAiProfiles;

public static class ListAiProfilesHandler
{
    public static Result<ListAiProfilesResponse> Handle(ListAiProfilesQuery query, IAiProfileCatalog catalog, IEmbeddingModel embeddings)
    {
        var space = embeddings.Space;

        return new ListAiProfilesResponse(
            catalog.DefaultChatProfile,
            [.. catalog.ListChatProfiles().Select(profile => new AiChatProfileResponse(profile.Name, profile.Provider, profile.Model, profile.Selectable, profile.Available))],
            new AiEmbeddingProfileResponse(space.Provider.ToLowerInvariant(), space.Model, space.Dimensions));
    }
}
