using System.ComponentModel.DataAnnotations;

namespace Lexora.Application.Common.Options;

public sealed class LockoutOptions
{
    [Range(1, 100)]
    public int MaxFailedAttempts { get; init; } = 5;

    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(15);
}
