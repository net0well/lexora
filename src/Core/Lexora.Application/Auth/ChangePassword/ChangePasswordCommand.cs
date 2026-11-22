namespace Lexora.Application.Auth.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword)
{
    public override string ToString() => "ChangePasswordCommand { }";
}
