using Lexora.Application.Abstractions.Identity;

namespace Lexora.TestKit.Fakes;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public static string LegacyHashOf(string password) => $"legacy::{password}";

    public string Hash(string password) => $"hash::{password}";

    public PasswordVerification Verify(string passwordHash, string password)
    {
        if (passwordHash == Hash(password))
        {
            return PasswordVerification.Succeeded;
        }

        return passwordHash == LegacyHashOf(password) ? PasswordVerification.SucceededRehashNeeded : PasswordVerification.Failed;
    }
}
