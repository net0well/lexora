using Lexora.Domain.Documents;

namespace Lexora.Application.Documents.ListDocuments;

public sealed record DocumentListItemResponse(
    Guid Id,
    string FileName,
    DocumentFormat Format,
    long SizeBytes,
    DocumentStatus Status,
    int ChunkCount,
    int EmbeddedChunkCount,
    DateTimeOffset UploadedAt,
    DateTimeOffset UpdatedAt);
