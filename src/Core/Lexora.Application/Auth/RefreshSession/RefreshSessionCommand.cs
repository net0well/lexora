namespace Lexora.Application.Auth.RefreshSession;

public sealed record RefreshSessionCommand(string? RefreshToken)
{
    public override string ToString() => "RefreshSessionCommand { }";
}
