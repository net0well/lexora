namespace Lexora.Application.Abstractions.Storage;

public sealed record FileDownload(Uri Url, DateTimeOffset ExpiresAt);
