namespace Lexora.Application.Abstractions.Identity;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string passwordHash, string password);
}
