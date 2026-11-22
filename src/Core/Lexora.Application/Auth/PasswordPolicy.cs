namespace Lexora.Application.Auth;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    public const string Description =
        "The password must have at least 12 characters, including upper and lower case letters and a number, and must not contain the e-mail.";

    public static bool IsStrong(string? password, string? email) =>
        password is { Length: >= MinimumLength }
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && (string.IsNullOrWhiteSpace(email) || !password.Contains(email.Trim(), StringComparison.OrdinalIgnoreCase));
}
