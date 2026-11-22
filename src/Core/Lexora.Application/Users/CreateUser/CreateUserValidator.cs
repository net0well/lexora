using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(command => command.Email).NotEmpty().WithErrorCode("user.email.invalid");
        RuleFor(command => command.Name).NotEmpty().WithErrorCode("user.name.required");
        RuleFor(command => command.Role).IsInEnum().WithErrorCode(UserNotifications.InvalidRoleKey);
        RuleFor(command => command.Password).StrongPassword(command => command.Email);
    }
}
