namespace Lexora.Infrastructure.Storage.Local;

public sealed record LocalDownloadTicket(string Key, string FileName, string ContentType);
