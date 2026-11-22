using FluentValidation;
using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Auth.SeedAdmin;
using Lexora.Application.Users;
using Lexora.Domain.Users;
using Lexora.Shared.Results;

namespace Lexora.Application.Auth.Services;

public sealed class SeedAdminService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IValidator<SeedAdminCommand> validator,
    TimeProvider timeProvider) : ISeedAdminService
{
    public async Task<Result<SeedAdminResponse>> SeedAsync(SeedAdminCommand command, CancellationToken cancellationToken)
    {
        if (await users.CountActiveAdminsAsync(cancellationToken) > 0)
        {
            return new SeedAdminResponse(Created: false, UserId: null);
        }

        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return Result.Failure<SeedAdminResponse>(
                validation.Errors.Select(failure => Notification.Validation(failure.ErrorCode, failure.ErrorMessage)));
        }

        var email = Email.Create(command.Email);

        if (email.IsSuccess && await users.ExistsByEmailAsync(email.Value, cancellationToken))
        {
            return Result.Failure<SeedAdminResponse>(UserNotifications.EmailDuplicate);
        }

        var created = User.Create(command.Email, command.Name, passwordHasher.Hash(command.Password!), UserRole.Admin, timeProvider.GetUtcNow());

        if (created.IsFailure)
        {
            return Result.Failure<SeedAdminResponse>(created.Notifications);
        }

        await users.AddAsync(created.Value, cancellationToken);

        return new SeedAdminResponse(Created: true, UserId: created.Value.Id);
    }
}
