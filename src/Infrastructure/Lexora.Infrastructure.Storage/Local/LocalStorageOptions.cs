using System.ComponentModel.DataAnnotations;

namespace Lexora.Infrastructure.Storage.Local;

public sealed class LocalStorageOptions
{
    public const string SectionName = "Storage:Local";

    [Required]
    public string RootPath { get; init; } = string.Empty;

    [Required]
    public Uri? DownloadBaseUrl { get; init; }
}
