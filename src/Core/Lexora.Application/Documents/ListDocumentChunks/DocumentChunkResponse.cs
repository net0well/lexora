namespace Lexora.Application.Documents.ListDocumentChunks;

public sealed record DocumentChunkResponse(
    Guid Id,
    int Index,
    string Content,
    int TokenCount,
    int? PageNumber,
    string? SheetName,
    string? SectionTitle);
