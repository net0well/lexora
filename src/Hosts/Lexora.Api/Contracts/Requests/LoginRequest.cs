using System.Text;

namespace Lexora.Api.Contracts.Requests;

public sealed record LoginRequest(string Email, string Password)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email);

        return true;
    }
}
