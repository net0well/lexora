using Lexora.Domain.Documents;

namespace Lexora.Application.Documents.UploadDocument;

public sealed record UploadDocumentResponse(Guid Id, string FileName, DocumentFormat Format, DocumentStatus Status, long SizeBytes);
