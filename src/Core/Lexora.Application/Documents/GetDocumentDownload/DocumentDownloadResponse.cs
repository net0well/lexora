namespace Lexora.Application.Documents.GetDocumentDownload;

public sealed record DocumentDownloadResponse(Uri Url, DateTimeOffset ExpiresAt);
