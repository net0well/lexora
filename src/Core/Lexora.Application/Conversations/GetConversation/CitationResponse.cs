namespace Lexora.Application.Conversations.GetConversation;

public sealed record CitationResponse(
    int Index,
    Guid DocumentId,
    string DocumentName,
    int? PageNumber,
    string? SheetName,
    string Excerpt,
    double Score);
