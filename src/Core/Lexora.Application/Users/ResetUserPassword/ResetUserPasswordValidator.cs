using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Users.ResetUserPassword;

public sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator() => RuleFor(command => command.NewPassword).StrongPassword();
}
