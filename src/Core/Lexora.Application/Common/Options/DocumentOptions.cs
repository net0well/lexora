using System.ComponentModel.DataAnnotations;

namespace Lexora.Application.Common.Options;

public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; init; } = 52_428_800;

    [Range(1, long.MaxValue)]
    public long MaxTotalStorageBytes { get; init; } = 21_474_836_480;
}
