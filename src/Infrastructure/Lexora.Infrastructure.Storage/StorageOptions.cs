namespace Lexora.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public StorageProvider Provider { get; init; } = StorageProvider.S3;

    public TimeSpan DownloadUrlLifetime { get; init; } = TimeSpan.FromMinutes(5);
}
