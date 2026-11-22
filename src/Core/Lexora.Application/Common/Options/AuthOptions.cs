using System.ComponentModel.DataAnnotations;

namespace Lexora.Application.Common.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    [Required]
    public LockoutOptions Lockout { get; init; } = new();
}
