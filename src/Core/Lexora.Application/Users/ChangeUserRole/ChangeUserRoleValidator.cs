using FluentValidation;

namespace Lexora.Application.Users.ChangeUserRole;

public sealed class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleValidator() =>
        RuleFor(command => command.Role).IsInEnum().WithErrorCode(UserNotifications.InvalidRoleKey);
}
