using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.Mappers;
using Lexora.Application.Common.Options;
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
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var user = email.IsSuccess ? await users.GetByEmailAsync(email.Value, cancellationToken) : null;

        if (user is null)
        {
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
