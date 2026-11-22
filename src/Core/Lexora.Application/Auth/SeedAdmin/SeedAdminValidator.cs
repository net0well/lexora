using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Auth.SeedAdmin;

public sealed class SeedAdminValidator : AbstractValidator<SeedAdminCommand>
{
    public SeedAdminValidator()
    {
        RuleFor(command => command.Email).NotEmpty().WithErrorCode("user.email.invalid");
        RuleFor(command => command.Name).NotEmpty().WithErrorCode("user.name.required");
        RuleFor(command => command.Password).StrongPassword(command => command.Email);
    }
}
