namespace Lexora.Application.Ai.ListAiProfiles;

public sealed record ListAiProfilesResponse(
    string DefaultChatProfile,
    IReadOnlyList<AiChatProfileResponse> ChatProfiles,
    AiEmbeddingProfileResponse Embedding);
