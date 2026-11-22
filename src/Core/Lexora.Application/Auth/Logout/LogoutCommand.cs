namespace Lexora.Application.Auth.Logout;

public sealed record LogoutCommand(string? RefreshToken)
{
    public override string ToString() => "LogoutCommand { }";
}
