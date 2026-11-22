namespace Lexora.Application.Abstractions.Identity;

public enum PasswordVerification
{
    Failed = 1,
    Succeeded = 2,
    SucceededRehashNeeded = 3
}
