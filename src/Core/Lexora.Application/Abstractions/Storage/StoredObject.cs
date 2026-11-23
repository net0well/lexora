namespace Lexora.Application.Abstractions.Storage;

public sealed record StoredObject(string Key, DateTimeOffset LastModified);
