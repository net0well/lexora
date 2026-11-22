using System.Security.Cryptography;
using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Auth.ChangePassword;
using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.Logout;
using Lexora.Application.Auth.Mappers;
using Lexora.Application.Auth.RefreshSession;
using Lexora.Application.Common.Options;
using Lexora.Application.Common.Validation;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Auth.Services;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    ISecretTokenGenerator secretTokenGenerator,
    ICurrentUser currentUser,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private static string? _decoyPasswordHash;

    public async Task<Result<LoginResponse>> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var user = email.IsSuccess ? await users.GetByEmailAsync(email.Value, cancellationToken) : null;

        if (user is null)
        {
            VerifyAgainstDecoy(command.Password);
            logger.SignInRejectedUnknownAccount();
            return Result.Failure<LoginResponse>(AuthNotifications.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();

        if (user.IsLockedOut(now))
        {
            logger.SignInRejected(user.Id, "locked out");
            return Result.Failure<LoginResponse>(AuthNotifications.LockedOut);
        }

        if (!user.IsActive)
        {
            VerifyAgainstDecoy(command.Password);
            logger.SignInRejected(user.Id, "inactive");
            return Result.Failure<LoginResponse>(AuthNotifications.InvalidCredentials);
        }

        var verification = passwordHasher.Verify(user.PasswordHash, command.Password);

        if (verification == PasswordVerification.Failed)
        {
            return RejectWrongPassword(user, now);
        }

        if (verification == PasswordVerification.SucceededRehashNeeded)
        {
            user.ChangePassword(passwordHasher.Hash(command.Password), now);
        }

        user.RegisterSuccessfulLogin(now);

        var refreshToken = secretTokenGenerator.Generate();
        var session = RefreshToken.Issue(user.Id, refreshToken.Hash, now + options.Value.RefreshTokenLifetime, now);
        await refreshTokens.AddAsync(session, cancellationToken);

        var accessToken = accessTokenIssuer.Issue(user);
        logger.SignedIn(user.Id);

        return new LoginResponse(accessToken.Value, accessToken.ExpiresAt, user.ToAuthenticatedUser(), refreshToken.Value, session.ExpiresAt);
    }

    public async Task<Result<RefreshSessionResponse>> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Failure<RefreshSessionResponse>(AuthNotifications.InvalidRefreshToken);
        }

        var current = await refreshTokens.GetByHashAsync(secretTokenGenerator.Hash(command.RefreshToken), cancellationToken);

        if (current is null)
        {
            return Result.Failure<RefreshSessionResponse>(AuthNotifications.InvalidRefreshToken);
        }

        var now = timeProvider.GetUtcNow();

        if (current.WasRotated)
        {
            await RevokeFamilyAsync(current, now, cancellationToken);
            return Result.Failure<RefreshSessionResponse>(AuthNotifications.InvalidRefreshToken);
        }

        if (!current.IsActive(now))
        {
            return Result.Failure<RefreshSessionResponse>(AuthNotifications.InvalidRefreshToken);
        }

        var user = await users.GetByIdAsync(current.UserId, cancellationToken);

        if (user is not { IsActive: true })
        {
            current.Revoke(RefreshTokenRevocationReason.UserDeactivated, now);
            return Result.Failure<RefreshSessionResponse>(AuthNotifications.UserInactive);
        }

        var nextToken = secretTokenGenerator.Generate();
        var replacement = current.Rotate(nextToken.Hash, now + options.Value.RefreshTokenLifetime, now);

        if (replacement.IsFailure)
        {
            return Result.Failure<RefreshSessionResponse>(replacement.Notifications);
        }

        await refreshTokens.AddAsync(replacement.Value, cancellationToken);

        var accessToken = accessTokenIssuer.Issue(user);

        return new RefreshSessionResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            user.ToAuthenticatedUser(),
            nextToken.Value,
            replacement.Value.ExpiresAt);
    }

    public async Task<Result> LogoutAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Success();
        }

        var token = await refreshTokens.GetByHashAsync(secretTokenGenerator.Hash(command.RefreshToken), cancellationToken);

        if (token is not null && token.UserId == currentUser.UserId)
        {
            token.Revoke(RefreshTokenRevocationReason.Logout, timeProvider.GetUtcNow());
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthNotifications.InvalidCredentials);
        }

        if (passwordHasher.Verify(user.PasswordHash, command.CurrentPassword) == PasswordVerification.Failed)
        {
            return Result.Failure(AuthNotifications.CurrentPasswordInvalid);
        }

        if (!PasswordPolicy.IsStrong(command.NewPassword, user.Email.Value))
        {
            return Result.Failure(Notification.Validation(PasswordRuleExtensions.WeakPasswordKey, PasswordPolicy.Description));
        }

        var now = timeProvider.GetUtcNow();
        user.ChangePassword(passwordHasher.Hash(command.NewPassword), now);

        foreach (var token in await refreshTokens.ListUnrevokedByUserAsync(user.Id, cancellationToken))
        {
            token.Revoke(RefreshTokenRevocationReason.PasswordChanged, now);
        }

        return Result.Success();
    }

    // Rejections that skip the real password check still pay for one, so response time does not reveal
    // whether the e-mail belongs to an active account.
    private void VerifyAgainstDecoy(string? password)
    {
        var decoy = _decoyPasswordHash ??= passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        passwordHasher.Verify(decoy, password ?? string.Empty);
    }

    private async Task RevokeFamilyAsync(RefreshToken reused, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var family = await refreshTokens.ListUnrevokedByFamilyAsync(reused.FamilyId, cancellationToken);

        foreach (var token in family)
        {
            token.Revoke(RefreshTokenRevocationReason.ReuseDetected, now);
        }

        logger.RefreshTokenReuseDetected(reused.UserId, reused.FamilyId);
    }

    private Result<LoginResponse> RejectWrongPassword(User user, DateTimeOffset now)
    {
        var lockout = options.Value.Lockout;
        user.RegisterFailedLogin(now, lockout.MaxFailedAttempts, lockout.Duration);

        if (user.IsLockedOut(now))
        {
            logger.LockedOut(user.Id);
            return Result.Failure<LoginResponse>(AuthNotifications.LockedOut);
        }

        logger.SignInRejected(user.Id, "wrong password");
        return Result.Failure<LoginResponse>(AuthNotifications.InvalidCredentials);
    }
}
