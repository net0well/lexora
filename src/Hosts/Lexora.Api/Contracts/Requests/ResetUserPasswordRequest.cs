namespace Lexora.Api.Contracts.Requests;

public sealed record ResetUserPasswordRequest(string NewPassword)
{
    public override string ToString() => "ResetUserPasswordRequest { }";
}
