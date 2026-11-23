namespace Lexora.Application.Abstractions.Ai;

public sealed record AiChatProfile(string Name, string Provider, string Model, bool Selectable, bool Available);
