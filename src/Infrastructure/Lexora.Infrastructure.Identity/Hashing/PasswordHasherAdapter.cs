using Lexora.Application.Abstractions.Identity;
using Lexora.Shared.Guards;
using Microsoft.AspNetCore.Identity;

namespace Lexora.Infrastructure.Identity.Hashing;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly PasswordHashSubject _subject = new();

    private readonly PasswordHasher<PasswordHashSubject> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(_subject, Guard.NotNullOrWhiteSpace(password));

    public PasswordVerification Verify(string passwordHash, string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrEmpty(password))
        {
            return PasswordVerification.Failed;
        }

        try
        {
            return _hasher.VerifyHashedPassword(_subject, passwordHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordVerification.Succeeded,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SucceededRehashNeeded,
                _ => PasswordVerification.Failed,
            };
        }
        catch (FormatException)
        {
            return PasswordVerification.Failed;
        }
    }
}
