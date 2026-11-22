using Lexora.Shared.Results;

namespace Lexora.Application.KnowledgeBases;

public static class KnowledgeBaseNotifications
{
    public static Notification NotFound { get; } = Notification.NotFound("knowledgeBase.notFound", "Knowledge base not found.");

    public static Notification NameDuplicate { get; } =
        Notification.Conflict("knowledgeBase.name.duplicate", "A knowledge base with this name already exists.");
}
