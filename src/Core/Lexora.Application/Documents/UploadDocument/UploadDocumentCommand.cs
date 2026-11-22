namespace Lexora.Application.Documents.UploadDocument;

public sealed record UploadDocumentCommand(Guid KnowledgeBaseId, string FileName, string? DeclaredContentType, long Length, Stream Content);
