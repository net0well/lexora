namespace Lexora.Application.Documents.Abstractions;

public sealed record StoredDocumentFile(string StorageKey, string FileName, string ContentType);
