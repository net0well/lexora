using System.Text;

namespace Lexora.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email);

        return true;
    }
}
