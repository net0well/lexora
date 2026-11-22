using System.Text;

namespace Lexora.Application.Auth.SeedAdmin;

public sealed record SeedAdminCommand(string? Email, string? Name, string? Password)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email).Append(", Name = ").Append(Name);

        return true;
    }
}
