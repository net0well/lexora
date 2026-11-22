using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.UnitTests.Auth;

internal sealed class SampleValidator : AbstractValidator<SampleInput>
{
    public SampleValidator() => RuleFor(input => input.Password).StrongPassword(input => input.Email);
}
