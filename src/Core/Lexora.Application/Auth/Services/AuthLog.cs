using Microsoft.Extensions.Logging;

namespace Lexora.Application.Auth.Services;

internal static partial class AuthLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "User {UserId} signed in")]
    public static partial void SignedIn(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Sign-in rejected for an unknown or malformed e-mail")]
    public static partial void SignInRejectedUnknownAccount(this ILogger logger);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Sign-in rejected for user {UserId}: {Reason}")]
    public static partial void SignInRejected(this ILogger logger, Guid userId, string reason);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "User {UserId} locked out after repeated failed sign-ins")]
    public static partial void LockedOut(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Refresh token reuse detected for user {UserId}; session family {FamilyId} revoked")]
    public static partial void RefreshTokenReuseDetected(this ILogger logger, Guid userId, Guid familyId);
}
