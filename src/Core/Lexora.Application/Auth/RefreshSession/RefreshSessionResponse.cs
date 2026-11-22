using System.Text;

namespace Lexora.Application.Auth.RefreshSession;

public sealed record RefreshSessionResponse(
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
