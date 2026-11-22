using System.ComponentModel.DataAnnotations;

namespace Lexora.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "lexora";

    [Required]
    public string Audience { get; init; } = "lexora";

    [Required]
    public string SigningKey { get; init; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);
}
