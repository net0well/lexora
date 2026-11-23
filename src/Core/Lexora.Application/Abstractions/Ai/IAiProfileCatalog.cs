namespace Lexora.Application.Abstractions.Ai;

public interface IAiProfileCatalog
{
    string DefaultChatProfile { get; }

    IReadOnlyList<AiChatProfile> ListChatProfiles();

    bool IsSelectable(string name);
}
