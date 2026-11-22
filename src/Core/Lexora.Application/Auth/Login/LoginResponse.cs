using System.Text;

namespace Lexora.Application.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("User = ").Append(User).Append(", ExpiresAt = ").Append(ExpiresAt);

        return true;
    }
}
