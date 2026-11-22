using System.ComponentModel.DataAnnotations;

namespace Lexora.Infrastructure.Storage.S3;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    [Required]
    public Uri? ServiceUrl { get; init; }

    public Uri? PublicUrl { get; init; }

    [Required]
    public string Bucket { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string Region { get; init; } = "us-east-1";

    public bool ForcePathStyle { get; init; } = true;
}
