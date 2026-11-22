namespace Lexora.Application.Abstractions.Identity;

public interface ISecretTokenGenerator
{
    SecretToken Generate();

    string Hash(string token);
}
