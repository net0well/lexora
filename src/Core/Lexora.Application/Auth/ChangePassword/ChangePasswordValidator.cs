using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Auth.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty().WithErrorCode("auth.password.required");
        RuleFor(command => command.NewPassword).StrongPassword();
    }
}
