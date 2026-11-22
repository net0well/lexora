using System.Text;

namespace Lexora.Application.Users.ResetUserPassword;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("UserId = ").Append(UserId);

        return true;
    }
}
