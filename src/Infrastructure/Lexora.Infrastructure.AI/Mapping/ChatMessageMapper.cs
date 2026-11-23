using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Mapping;

public static class ChatMessageMapper
{
    public static List<ChatMessage> ToMessages(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ChatMessage> messages = [];

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, request.SystemPrompt));
        }

        messages.AddRange(request.Turns.Select(turn => new ChatMessage(RoleOf(turn.Role), turn.Content)));

        return messages;
    }

    public static ChatOptions ToOptions(ChatRequest request, AiProfileSettings profile)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profile);

        return new ChatOptions
        {
            ModelId = profile.Model,
            Temperature = request.Temperature ?? profile.Temperature ?? AiChatSettings.DefaultTemperature,
            MaxOutputTokens = request.MaxOutputTokens ?? profile.MaxOutputTokens ?? AiChatSettings.DefaultMaxOutputTokens,
        };
    }

    public static string ProviderNameOf(AiProviderKind provider) => provider.ToString().ToLowerInvariant();

    private static ChatRole RoleOf(ChatTurnRole role) => role switch
    {
        ChatTurnRole.User => ChatRole.User,
        ChatTurnRole.Assistant => ChatRole.Assistant,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown chat turn role."),
    };
}
