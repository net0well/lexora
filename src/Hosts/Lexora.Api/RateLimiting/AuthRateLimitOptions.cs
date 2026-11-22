using System.ComponentModel.DataAnnotations;

namespace Lexora.Api.RateLimiting;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    [Range(1, 10_000)]
    public int LoginPermitsPerMinute { get; init; } = 10;

    [Range(1, 10_000)]
    public int RefreshPermitsPerMinute { get; init; } = 30;
}
