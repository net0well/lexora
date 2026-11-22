using FluentValidation;

namespace Lexora.Application.Auth.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command.Email).NotEmpty().WithErrorCode("user.email.invalid");
        RuleFor(command => command.Password).NotEmpty().WithErrorCode("auth.password.required");
    }
}
