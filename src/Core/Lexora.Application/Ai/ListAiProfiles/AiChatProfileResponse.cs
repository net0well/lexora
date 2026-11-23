namespace Lexora.Application.Ai.ListAiProfiles;

public sealed record AiChatProfileResponse(string Name, string Provider, string Model, bool Selectable, bool Available);
