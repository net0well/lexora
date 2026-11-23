using Lexora.Application.Conversations.GetConversation;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Mappers;

public static class ConversationMapper
{
    public static GetConversationResponse ToResponse(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        return new GetConversationResponse(
            conversation.Id,
            conversation.KnowledgeBaseId,
            conversation.Title,
            [.. conversation.Messages.Select(ToResponse)],
            conversation.CreatedAt,
            conversation.UpdatedAt);
    }

    public static ConversationMessageResponse ToResponse(ConversationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ConversationMessageResponse(
            message.Id,
            message.Role,
            message.Content,
            [.. message.Citations.Select(ToResponse)],
            message.Provider,
            message.Model,
            message.Usage.InputTokens,
            message.Usage.OutputTokens,
            message.LatencyMs,
            message.CreatedAt);
    }

    public static CitationResponse ToResponse(Citation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        return new CitationResponse(
            citation.Index,
            citation.DocumentId,
            citation.DocumentName,
            citation.PageNumber,
            citation.SheetName,
            citation.Excerpt,
            citation.Score);
    }
}
