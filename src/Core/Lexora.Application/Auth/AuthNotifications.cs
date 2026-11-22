using Lexora.Shared.Results;

namespace Lexora.Application.Auth;

public static class AuthNotifications
{
    public static Notification InvalidCredentials { get; } =
        Notification.Unauthorized("auth.invalidCredentials", "The e-mail or password is incorrect.");

    public static Notification LockedOut { get; } =
        Notification.Unauthorized("auth.lockedOut", "Too many failed attempts. Try again later.");

    public static Notification UserInactive { get; } =
        Notification.Unauthorized("auth.userInactive", "The user account is inactive.");

    public static Notification InvalidRefreshToken { get; } =
        Notification.Unauthorized("auth.refreshToken.invalid", "The session is no longer valid. Sign in again.");

    public static Notification CurrentPasswordInvalid { get; } =
        Notification.Validation("auth.password.currentInvalid", "The current password is incorrect.");
}
