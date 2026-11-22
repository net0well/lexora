using Lexora.Domain.Documents;

namespace Lexora.Application.Documents.GetDocument;

public sealed record GetDocumentResponse(
    Guid Id,
    Guid KnowledgeBaseId,
    string FileName,
    DocumentFormat Format,
    string ContentType,
    long SizeBytes,
    DocumentStatus Status,
    int ProcessingVersion,
    int? PageCount,
    int ChunkCount,
    int EmbeddedChunkCount,
    string? FailureReason,
    Guid UploadedBy,
    DateTimeOffset UploadedAt,
    DateTimeOffset? IndexedAt,
    DateTimeOffset UpdatedAt);
