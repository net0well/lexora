using FluentValidation;
using Lexora.Application.Auth;

namespace Lexora.Application.Common.Validation;

public static class PasswordRuleExtensions
{
    public const string WeakPasswordKey = "auth.password.weak";

    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(this IRuleBuilder<T, string?> rule, Func<T, string?>? emailOf = null) =>
        rule.Must((model, password) => PasswordPolicy.IsStrong(password, emailOf?.Invoke(model)))
            .WithErrorCode(WeakPasswordKey)
            .WithMessage(PasswordPolicy.Description);
}
