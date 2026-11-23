using System.ComponentModel.DataAnnotations;

namespace Lexora.Api.RateLimiting;

public sealed class ChatRateLimitOptions
{
    public const string SectionName = "RateLimiting:Chat";

    [Range(1, 10_000)]
    public int PermitsPerMinute { get; init; } = 20;
}
