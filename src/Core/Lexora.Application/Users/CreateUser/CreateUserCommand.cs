using System.Text;
using Lexora.Domain.Users;

namespace Lexora.Application.Users.CreateUser;

public sealed record CreateUserCommand(string Email, string Name, UserRole Role, string Password)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Email = ").Append(Email).Append(", Name = ").Append(Name).Append(", Role = ").Append(Role);

        return true;
    }
}
