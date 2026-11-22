using Lexora.Shared.Results;

namespace Lexora.Application.Documents;

public static class DocumentNotifications
{
    public static Notification NotFound { get; } = Notification.NotFound("document.notFound", "Document not found.");

    public static Notification Duplicate { get; } =
        Notification.Conflict("document.duplicate", "This file already exists in the knowledge base.");
}
