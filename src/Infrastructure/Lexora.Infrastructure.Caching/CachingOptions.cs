using System.ComponentModel.DataAnnotations;

namespace Lexora.Infrastructure.Caching;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    [Required]
    public string InstanceName { get; init; } = "lexora:";

    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan LocalExpiration { get; init; } = TimeSpan.FromMinutes(5);
}
