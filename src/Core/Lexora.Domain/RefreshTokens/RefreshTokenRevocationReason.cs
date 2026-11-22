namespace Lexora.Domain.RefreshTokens;

public enum RefreshTokenRevocationReason
{
    Rotated = 1,
    Logout = 2,
    PasswordChanged = 3,
    ReuseDetected = 4,
    UserDeactivated = 5
}
