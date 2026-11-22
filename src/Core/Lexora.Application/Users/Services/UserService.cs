using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Auth;
using Lexora.Application.Common.Validation;
using Lexora.Application.Users.ActivateUser;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.DeactivateUser;
using Lexora.Application.Users.Mappers;
using Lexora.Application.Users.ResetUserPassword;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Shared.Results;

namespace Lexora.Application.Users.Services;

public sealed class UserService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IUserService
{
    public async Task<Result<UserResponse>> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);

        if (email.IsSuccess && await users.ExistsByEmailAsync(email.Value, cancellationToken))
        {
            return Result.Failure<UserResponse>(UserNotifications.EmailDuplicate);
        }

        var created = User.Create(command.Email, command.Name, passwordHasher.Hash(command.Password), command.Role, timeProvider.GetUtcNow());

        if (created.IsFailure)
        {
            return Result.Failure<UserResponse>(created.Notifications);
        }

        await users.AddAsync(created.Value, cancellationToken);

        return created.Value.ToResponse();
    }

    public async Task<Result<UserResponse>> ChangeRoleAsync(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserNotifications.NotFound);
        }

        if (command.Role != UserRole.Admin && await IsLastActiveAdminAsync(user, cancellationToken))
        {
            return Result.Failure<UserResponse>(UserNotifications.LastAdmin);
        }

        user.ChangeRole(command.Role, timeProvider.GetUtcNow());

        return user.ToResponse();
    }

    public async Task<Result<UserResponse>> DeactivateAsync(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserNotifications.NotFound);
        }

        if (await IsLastActiveAdminAsync(user, cancellationToken))
        {
            return Result.Failure<UserResponse>(UserNotifications.LastAdmin);
        }

        var now = timeProvider.GetUtcNow();
        user.Deactivate(now);
        await RevokeSessionsAsync(user, RefreshTokenRevocationReason.UserDeactivated, now, cancellationToken);

        return user.ToResponse();
    }

    public async Task<Result<UserResponse>> ActivateAsync(ActivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserNotifications.NotFound);
        }

        user.Activate(timeProvider.GetUtcNow());

        return user.ToResponse();
    }

    public async Task<Result> ResetPasswordAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserNotifications.NotFound);
        }

        if (!PasswordPolicy.IsStrong(command.NewPassword, user.Email.Value))
        {
            return Result.Failure(Notification.Validation(PasswordRuleExtensions.WeakPasswordKey, PasswordPolicy.Description));
        }

        var now = timeProvider.GetUtcNow();
        user.ChangePassword(passwordHasher.Hash(command.NewPassword), now);
        await RevokeSessionsAsync(user, RefreshTokenRevocationReason.PasswordChanged, now, cancellationToken);

        return Result.Success();
    }

    private async Task<bool> IsLastActiveAdminAsync(User user, CancellationToken cancellationToken) =>
        user.Role == UserRole.Admin && user.IsActive && await users.CountActiveAdminsAsync(cancellationToken) <= 1;

    private async Task RevokeSessionsAsync(User user, RefreshTokenRevocationReason reason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var token in await refreshTokens.ListUnrevokedByUserAsync(user.Id, cancellationToken))
        {
            token.Revoke(reason, now);
        }
    }
}
