namespace Lexora.Api.Contracts.Requests;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword)
{
    public override string ToString() => "ChangePasswordRequest { }";
}
