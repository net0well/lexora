using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lexora.Infrastructure.Identity.Tokens;

public sealed class JwtSigningKeyProvider(IOptions<JwtOptions> options)
{
    public const int MinimumKeyBytes = 32;

    public SecurityKey Key { get; } = new SymmetricSecurityKey(Decode(options.Value.SigningKey));

    public static bool IsValid(string? signingKey) => TryDecode(signingKey, out _);

    private static byte[] Decode(string signingKey) =>
        TryDecode(signingKey, out var bytes)
            ? bytes
            : throw new InvalidOperationException(
                $"'{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}' must be a base64 value of at least {MinimumKeyBytes} bytes.");

    private static bool TryDecode(string? signingKey, out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return false;
        }

        var buffer = new byte[signingKey.Length];

        if (!Convert.TryFromBase64String(signingKey, buffer, out var written) || written < MinimumKeyBytes)
        {
            return false;
        }

        bytes = buffer[..written];

        return true;
    }
}
