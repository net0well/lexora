using Lexora.Application.Abstractions.Identity;

namespace Lexora.TestKit.Fakes;

public sealed class FakeSecretTokenGenerator : ISecretTokenGenerator
{
    private int _generated;

    public SecretToken Generate()
    {
        var value = $"refresh-token-{Interlocked.Increment(ref _generated)}";

        return new SecretToken(value, Hash(value));
    }

    public string Hash(string token) => $"sha256::{token}";
}
