using Lexora.Domain.Documents;

namespace Lexora.Application.Documents.ReprocessDocument;

public sealed record ReprocessDocumentResponse(Guid Id, DocumentStatus Status, int ProcessingVersion);
