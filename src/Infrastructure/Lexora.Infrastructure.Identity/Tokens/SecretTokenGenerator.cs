using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Lexora.Application.Abstractions.Identity;
using Lexora.Shared.Guards;

namespace Lexora.Infrastructure.Identity.Tokens;

public sealed class SecretTokenGenerator : ISecretTokenGenerator
{
    public const int TokenBytes = 64;

    public SecretToken Generate()
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

        return new SecretToken(value, Hash(value));
    }

    public string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Guard.NotNullOrWhiteSpace(token))));
}
