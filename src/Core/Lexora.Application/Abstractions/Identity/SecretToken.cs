namespace Lexora.Application.Abstractions.Identity;

public sealed record SecretToken(string Value, string Hash)
{
    public override string ToString() => "SecretToken { }";
}
